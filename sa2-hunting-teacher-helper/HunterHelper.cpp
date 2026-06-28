#include "pch.h"
#include "HunterHelper.h"
#include <algorithm>
#include <chrono>
#include <string>
#include <thread>
#include <vector>

UsercallFuncVoid(hAwardWin, (signed int* player), (player), 0x43E6D0, rESI);
UsercallFuncVoid(hExitHandler, (int a1, int a2, int a3), (a1, a2, a3), 0x4016D0, rECX, rEBX, rESI);
UsercallFuncVoid(hSetPhysicsAndGiveUpgrades, (ObjectMaster* character, int a2), (character, a2), (intptr_t)0x4599C0, rEAX, rECX);
FunctionHook<void*, const void*> hLoadStageHintsFile((intptr_t)0x73B6C0);
FunctionHook<void, EmeraldManager*> hLoadEmeraldLocations((intptr_t)0x7380A0);
FunctionHook<void> hLoadLevel((intptr_t)0x43C970);

void HunterHelper::Init() {
	HunterHelper::HookActiveWindow();
	if (!HunterHelper::TeacherDataState) {
		HunterHelper::OpenSharedMemory();
	}

	hLoadLevel.Hook(HunterHelper::LoadLevel);
	hAwardWin.Hook(HunterHelper::AwardWin);
	hSetPhysicsAndGiveUpgrades.Hook(HunterHelper::SetPhysicsAndGiveUpgrades);
	hLoadStageHintsFile.Hook(HunterHelper::EmeraldHintsFileLoaderInterceptor);
	hLoadEmeraldLocations.Hook(HunterHelper::LoadEmeraldLocations);
	hExitHandler.Hook(HunterHelper::ExitHandler);

	HunterHelper::InitDebugModeCompat();
}

void HunterHelper::InitDebugModeCompat() {
	HMODULE debugMode = GetModuleHandleA("SA2-Debug-Mode.dll");
	if (!debugMode) {
		return;
	}

	DebugModeGetSaveApi getApi = reinterpret_cast<DebugModeGetSaveApi>(GetProcAddress(debugMode, "DebugMode_GetSaveApi"));
	if (!getApi) {
		return;
	}

	HunterHelper::SaveStates = getApi();
}

void HunterHelper::HookActiveWindow() {
	FindActiveWindow param{ GetCurrentProcessId(), NULL };
	EnumWindows(HunterHelper::EnumWindowsProc, (LPARAM)&param);

	if (!param.hwnd) {
		MessageBox(NULL, L"Failed to hook active process!", L"Error!", MB_OK | MB_ICONERROR);
		exit(1);
	}

	HunterHelper::OldWndProc = (WNDPROC)SetWindowLong(param.hwnd, GWLP_WNDPROC, (LONG_PTR)HunterHelper::WndProc);
}

BOOL CALLBACK HunterHelper::EnumWindowsProc(HWND hwnd, LPARAM lParam) {
	auto* data = (FindActiveWindow*)lParam;

	DWORD pid = 0;
	GetWindowThreadProcessId(hwnd, &pid);
	if (pid != data->pid) {
		return TRUE;
	}

	if (!IsWindowVisible(hwnd)) {
		return TRUE;
	}

	if (GetWindow(hwnd, GW_OWNER) != NULL) {
		return TRUE;
	}

	LONG exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
	if (exStyle & WS_EX_TOOLWINDOW) {
		return TRUE;
	}

	data->hwnd = hwnd;
	return FALSE;
}

void HunterHelper::LoadLevel() {
	if (CurrentLevel == HunterHelper::TeacherDataState->currentLevel) {
		HunterHelper::TeacherDataState->levelLoading = true;
	}

	if (CurrentLevel != LevelIDs_MadSpace) {
		HunterHelper::HintsBuffer = nullptr;
	}

	hLoadLevel.Original();
}

void HunterHelper::AwardWin(signed int* player) {
	if (HunterHelper::TeacherDataState->currentLevel != CurrentLevel) {
		hAwardWin.Original(player);
		return;
	}

	HunterHelper::TeacherDataState->inWinScreen = true;
	if (HunterHelper::TeacherDataState->sequenceComplete) {
		hAwardWin.Original(player);
		return;
	}

	StopTimer = 1;
	EmeraldManagerObj->Action = 4;
	EmeraldManagerObj->EmeraldsSpawned = 0;
	EmeraldManagerObj->Piece1.id = 0xFF;
	EmeraldManagerObj->Piece2.id = 0xFF;
	EmeraldManagerObj->Piece3.id = 0xFF;
	GameState = HunterHelper::TeacherDataState->backToMenu ? GameStates_ReturnToMenu_1 : GameStates_RestartLevel_NoLifeLost;

	if (!HunterHelper::TeacherDataState->backToMenu) {
		InGameFrameCount = 0;
		if (HunterHelper::TeacherDataState->timerReset) {
			TimerMinutes = TimerSeconds = TimerFrames = 0;
			TimerMinutesContinue = TimerSecondsContinue = TimerFramesContinue = 0;
		} else {
			TimerMinutesContinue = TimerMinutes;
			TimerSecondsContinue = TimerSeconds;
			TimerFramesContinue = TimerFrames;
		}
	}
}

void HunterHelper::SetPhysicsAndGiveUpgrades(ObjectMaster* character, int a2) {
	hSetPhysicsAndGiveUpgrades.Original(character, a2);
	if (CurrentLevel == HunterHelper::TeacherDataState->currentLevel) {
		if (CurrentLevel == LevelIDs_PumpkinHill) {
			MainCharObj2[0]->Upgrades |= Upgrades_KnucklesShovelClaw;
		}

		if (CurrentLevel == LevelIDs_EggQuarters) {
			MainCharObj2[0]->Upgrades |= Upgrades_RougePickNails;
		}
	}
}

bool HunterHelper::IsShiftJISCharacter(uint8_t leadByte, uint8_t trailByte) {
	return ((leadByte >= 0x81 && leadByte <= 0x9F) || (leadByte >= 0xE0 && leadByte <= 0xFC))
		&& ((trailByte >= 0x40 && trailByte <= 0x7E) || (trailByte >= 0x80 && trailByte <= 0xFC));
}

void HunterHelper::ReverseShiftJISHint(uint8_t* hintStart, uint8_t* hintEnd) {
	if (!hintStart || !hintEnd || hintStart >= hintEnd) {
		return;
	}

	struct Token { uint8_t byte0, byte1; bool doubleChar; };
	std::vector<Token> tokens;

	uint8_t* c = hintStart;
	while (c < hintEnd) {
		uint8_t byte0 = *c++;

		// ignore game control bytes
		if (byte0 == 0x0E || byte0 == 0x0F) {
			tokens.push_back({ byte0, 0, false });
		} else if (*c != NULL && HunterHelper::IsShiftJISCharacter(byte0, *c)) {
			uint8_t byte1 = *c++;
			tokens.push_back({ byte0, byte1, true });
;		} else {
			tokens.push_back({ byte0, 0, false });
		}
	}

	std::reverse(tokens.begin(), tokens.end());
	uint8_t* out = hintStart;
	for (const auto& token : tokens) {
		*out++ = token.byte0;
		if (token.doubleChar) {
			*out++ = token.byte1;
		}
	}
}

void* HunterHelper::EmeraldHintsFileLoaderInterceptor(const void* hintsFileName) {
	if (CurrentLevel != LevelIDs_MadSpace) {
		HunterHelper::HintsBuffer = nullptr;
		return hLoadStageHintsFile.Original(hintsFileName);
	}

	void* data = hLoadStageHintsFile.Original(hintsFileName);
	HunterHelper::HintsBuffer = data;
	HunterHelper::HintsCurrentlyReversed = true;

	if (!HunterHelper::TeacherDataState->mspReversedHints) {
		HunterHelper::ApplyHintsFlip(data);
	}

	return data;
}

void HunterHelper::ApplyHintsFlip(void* data) {
	uint8_t* base = (uint8_t*)data;
	auto table = reinterpret_cast<uint32_t*>(base);
	for (size_t i = 0; i < HunterHelper::MAX_HINT_SIZE; ++i) {
		uint32_t off = table[i];
		if (off == HunterHelper::FILE_END_BITS) {
			break;
		}

		if (i % 3 != 0) {
			continue;
		}

		uint8_t* hintTextStart = base + off;
		// command options to skip
		if (*hintTextStart == NEW_LINE) {
			hintTextStart++;
			while (*hintTextStart != NULL && *hintTextStart != ' ' && *hintTextStart != ':') {
				hintTextStart++;
			}

			if (*hintTextStart != NULL) {
				hintTextStart++;
			}
		}

		// center command option
		if (*hintTextStart == CENTER_COMMAND) {
			hintTextStart++;
		}

		uint8_t* hintTextEnd = hintTextStart;
		while (*hintTextEnd != NULL && *hintTextEnd != NEW_LINE) {
			hintTextEnd++;
		}

		if (CurrentLanguage == Language_Japanese) {
			HunterHelper::ReverseShiftJISHint(hintTextStart, hintTextEnd);
		} else {
			std::reverse(hintTextStart, hintTextEnd);
		}
	}

	HunterHelper::HintsCurrentlyReversed = !HunterHelper::HintsCurrentlyReversed;
}

Emerald* HunterHelper::GetPieceById(EmeraldManager* emManager, int id) {
	byte idLowByte = id & 0xFF;
	Emerald* emeralds = nullptr;
	int emeraldsLength = 0;
	switch (idLowByte) {
		case 0:
		case 2:
		case 5:
			emeralds = emManager->Slot2Emeralds;
			emeraldsLength = emManager->Slot2ArrayLen;
			break;
		case 1:
		case 3:
			emeralds = emManager->Slot1Emeralds;
			emeraldsLength = emManager->Slot1ArrayLen;
			break;
		case 4:
		case 7:
		case 8:
			emeralds = emManager->Slot3Emeralds;
			emeraldsLength = emManager->Slot3ArrayLen;
			break;
		case 0xA:
			emeralds = emManager->EnemySlotEmeralds;
			emeraldsLength = emManager->EnemySlotArrayLen;
			break;
	}

	for (int i = 0; i < emeraldsLength; i++) {
		if (emeralds[i].id == id) {
			return &emeralds[i];
		}
	}

	MessageBox(NULL, L"Invalid ID Detected! Please report this along with the level and set that was last loaded.", L"Error!", MB_OK | MB_ICONERROR);
	exit(1);

	return nullptr;
}

void HunterHelper::LoadEmeraldLocations(EmeraldManager* emManager) {
	if (CurrentLevel != HunterHelper::TeacherDataState->currentLevel) {
		return hLoadEmeraldLocations.Original(emManager);
	}
	
	// if resetting pieces, wait for teacher to send new ones
	while (HunterHelper::TeacherDataState->inWinScreen || HunterHelper::TeacherDataState->levelLoading) {
		std::this_thread::sleep_for(std::chrono::milliseconds(100));
	}

	if (
		CurrentLevel == LevelIDs_MadSpace &&
		HunterHelper::HintsBuffer &&
		HunterHelper::TeacherDataState->mspReversedHints != HunterHelper::HintsCurrentlyReversed
	) {
		HunterHelper::ApplyHintsFlip(HunterHelper::HintsBuffer);
	}

	// This is a save state, just ignore it.
	if (HunterHelper::SaveStates && HunterHelper::SaveStates->HasPendingHuntingRestore()) {
		return hLoadEmeraldLocations.Original(emManager);
	}

	Life_Count[0] = 99;
	if (emManager->Piece1.id != HunterHelper::PIECE_COLLECTED) {
		Emerald* p1 = HunterHelper::GetPieceById(emManager, HunterHelper::TeacherDataState->p1Id);
		*&emManager->Piece1.id = *&p1->id;
		emManager->Piece1.v = p1->v;
		emManager->EmeraldsSpawned++;
	}

	if (emManager->Piece2.id != HunterHelper::PIECE_COLLECTED) {
		Emerald* p2 = HunterHelper::GetPieceById(emManager, HunterHelper::TeacherDataState->p2Id);
		*&emManager->Piece2.id = *&p2->id;
		emManager->Piece2.v = p2->v;
		emManager->EmeraldsSpawned++;
	}

	if (emManager->Piece3.id != HunterHelper::PIECE_COLLECTED) {
		Emerald* p3 = HunterHelper::GetPieceById(emManager, HunterHelper::TeacherDataState->p3Id);
		*&emManager->Piece3.id = *&p3->id;
		emManager->Piece3.v = p3->v;
		emManager->EmeraldsSpawned++;
	}
}

void HunterHelper::OpenSharedMemory() {
	HunterHelper::hMap = OpenFileMappingA(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, "SA2-Hunter-Teacher");
	if (!HunterHelper::hMap) {
		DWORD error = GetLastError();
		std::string errorMsg = std::string("Failed to access Hunter Teacher: ");
		MessageBox(NULL, (std::wstring(errorMsg.begin(), errorMsg.end()) + std::to_wstring(error)).c_str(), L"Error!", MB_OK | MB_ICONERROR);
		exit(1);
	}

	HunterHelper::TeacherDataState = (HunterTeacherData*)MapViewOfFile(HunterHelper::hMap, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, sizeof(HunterTeacherData));
}

void HunterHelper::CleanUp() {
	if (HunterHelper::TeacherDataState) {
		UnmapViewOfFile(HunterHelper::TeacherDataState);
		HunterHelper::TeacherDataState = nullptr;
	}

	if (HunterHelper::hMap) {
		CloseHandle(HunterHelper::hMap);
		HunterHelper::hMap = nullptr;
	}
}

void HunterHelper::ExitHandler(int a1, int a2, int a3) {
	HunterHelper::CleanUp();
	hExitHandler.Original(a1, a2, a3);
}

LRESULT __stdcall HunterHelper::WndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
	if (uMsg == WM_QUIT || (uMsg == WM_SYSCOMMAND && (wParam & 0xFFF0) == SC_CLOSE)) {
		HunterHelper::CleanUp();
	}

	return CallWindowProc(HunterHelper::OldWndProc, hWnd, uMsg, wParam, lParam);
}
