using sa2_hunting_teacher.Knuckles;
using sa2_hunting_teacher.Rouge;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace sa2_hunting_teacher;

public partial class SA2Manager : IDisposable {
	public const string SONIC_EXECUTABLE = "sonic2app";
	public const string HELPER_DLL_NAME = "hunting-teacher.helper.dll";
	private static MemoryMappedFile? MemoryMapper;
	private static bool CanRun = true;

	private IntPtr? sa2;
	private readonly Process targetProcess;
	private readonly HuntingLevel level;
	private readonly HuntingTeacherForm teacherForm;
	private readonly MemoryMappedViewAccessor sharedMemory;
	private readonly bool repetitionsInPlace;
	private HunterTeacherData HunterTeacherData;

	private SA2Manager(LevelRow selection, byte repetitions, HuntingTeacherForm teacherForm, bool repetitionsInPlace) {
		this.teacherForm = teacherForm;
		this.level = selection.Level switch {
			Level.WildCanyon => new WildCanyon(this, repetitions),
			Level.PumpkinHill => new PumpkinHill(this, repetitions),
			Level.AquaticMine => new AquaticMine(this, repetitions),
			Level.DeathChamber => new DeathChamber(this, repetitions),
			Level.MeteorHerd => new MeteorHerd(this, repetitions),
			Level.DryLagoon => new DryLagoon(this, repetitions),
			Level.EggQuarters => new EggQuarters(this, repetitions),
			Level.SecurityHall => new SecurityHall(this, repetitions),
			Level.MadSpace => new MadSpace(this, repetitions),
			_ => throw new ArgumentException("Invalid Level Selected!")
		};

		if (selection.CustomSequence != null) {
			this.level = new CustomHuntingLevel(this, repetitions, selection.CustomSequence, this.level.PieceToHintInstance);
		}

		Process[] processes = Process.GetProcessesByName(SONIC_EXECUTABLE);
		if (processes.Length < 1) {
			throw new ArgumentException("SA2 Is Not Running!");
		}

		teacherForm.Invoke(() => {
			HuntingTeacherForm.AddLogItem("Starting sequence run on " + this.level.ToString() + "...");
		});

		this.targetProcess = processes[0];
		this.sa2 = OpenProcess(
			ProcessAccessFlags.CreateThread |
			ProcessAccessFlags.QueryInformation |
			ProcessAccessFlags.VMOperation |
			ProcessAccessFlags.VMWrite |
			ProcessAccessFlags.VMRead,
			false,
			this.targetProcess.Id
		);

		if (this.sa2.GetValueOrDefault() == IntPtr.Zero) {
			this.HandleInjectionFailure();
		}

		SA2Manager.MemoryMapper ??= MemoryMappedFile.CreateOrOpen(
			"SA2-Hunter-Teacher",
			Marshal.SizeOf<HunterTeacherData>(),
			MemoryMappedFileAccess.ReadWrite
		);

		MspHints mspSelection = MspHints.ALTERNATING;
		bool backToMenu = false;
		bool timerReset = true;
		teacherForm.Invoke(() => {
			mspSelection = teacherForm.MspHintsSelection();
			backToMenu = teacherForm.BackToMenu();
			timerReset = teacherForm.TimerReset();
		});

		// Counter intuitively, we set reversed hints to true on ALTERNATING instead of ALTERNATING_REVERSED
		// because ApplySet will set reversed hints to the opposite for the alternating settings
		// so this value needs to be initially wrong when an alternating setting is set.
		bool reversedHints = mspSelection == MspHints.REVERSED || mspSelection == MspHints.ALTERNATING;

		this.repetitionsInPlace = repetitionsInPlace;
		this.sharedMemory = SA2Manager.MemoryMapper.CreateViewAccessor();
		this.ApplyDataDefaults(this.level.LevelId, reversedHints, backToMenu, timerReset);
		this.InjectDll();
	}

	private void ApplyDataDefaults(LevelId level, bool mspReversedHints, bool backToMenu, bool timerReset) {
		this.HunterTeacherData.currentLevel = (int)level;
		this.HunterTeacherData.inWinScreen = false;
		this.HunterTeacherData.sequenceComplete = false;
		this.HunterTeacherData.mspReversedHints = mspReversedHints;
		this.HunterTeacherData.backToMenu = backToMenu;
		this.HunterTeacherData.timerReset = timerReset;
		this.HunterTeacherData.p1Id = 0;
		this.HunterTeacherData.p2Id = 0;
		this.HunterTeacherData.p3Id = 0;
		this.sharedMemory.Write(0, ref this.HunterTeacherData);
	}

	public void ApplySet(Set set, int seqCount, int seqTotal, int currentRep) {
		this.HunterTeacherData.inWinScreen = false;
		this.HunterTeacherData.sequenceComplete = this.level.SequenceWillBeComplete();
		this.HunterTeacherData.p1Id = set.P1Id;
		this.HunterTeacherData.p2Id = set.P2Id;
		this.HunterTeacherData.p3Id = set.P3Id;
		this.HunterTeacherData.levelLoading = false;

		MspHints selection = MspHints.ALTERNATING;
		this.teacherForm.Invoke(() => selection = this.teacherForm.MspHintsSelection());
		if (selection == MspHints.ALTERNATING || selection == MspHints.ALTERNATING_REVERSED) {
			this.HunterTeacherData.mspReversedHints = !this.HunterTeacherData.mspReversedHints;
			if (currentRep == 1) {
				this.HunterTeacherData.mspReversedHints = selection == MspHints.ALTERNATING_REVERSED;
			}
		}

		if (!this.level.SequenceComplete()) {
			this.LogMessage($"Writing Set ({seqCount} / {seqTotal}) For Rep ({currentRep}): " + set);
		}
	}

	public bool IsLevelLoading() {
		return this.HunterTeacherData.levelLoading;
	}

	public bool IsInWinScreen() {
		if (this.HunterTeacherData.inWinScreen) {
			return true;
		}

		return false;
	}

	public bool RepetitionsInPlace() {
		return this.repetitionsInPlace;
	}

	public void LogMessage(string msg) {
		this.teacherForm.Invoke(() => {
			HuntingTeacherForm.AddLogItem(msg);
		});
	}

	private void HandleMemoryError(string msg) {
		this.teacherForm.Invoke(() => {
			HuntingTeacherForm.AddLogItem(msg);
			this.teacherForm.ResetBtn_Click(this.teacherForm, new EventArgs());
		});
	}

	public void Dispose() {
		if (SA2Manager.MemoryMapper != null) {
			SA2Manager.MemoryMapper.Dispose();
			SA2Manager.MemoryMapper = null;
		}

		this.CloseResource();
		GC.SuppressFinalize(this);
	}

	private void CloseResource() {
		if (this.sa2 != null) {
			CloseHandle((IntPtr) this.sa2);
			this.sa2 = null;
		}
	}

	private bool DllInjected() {
		try {
			for (int i = 0; i < this.targetProcess.Modules.Count; i++) {
				if (this.targetProcess.Modules[i].ModuleName.ToLower().Equals(HELPER_DLL_NAME)) {
					return true;
				}
			}
		} catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_ACCESS_DENIED) {
			this.CloseInjectionResources();
			throw new SA2ProcessAccessException(GetInjectionFailureMessage(ex.NativeErrorCode));
		}

		return false;
	}

	private void InjectDll() {
		if (this.DllInjected()) {
			return;
		}

		string dllPath = Path.Join(Directory.GetCurrentDirectory(), HELPER_DLL_NAME);
		IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
		IntPtr allocMemAddress = VirtualAllocEx((IntPtr)this.sa2!, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
		if (allocMemAddress == IntPtr.Zero) {
			this.HandleInjectionFailure();
		}

		bool success = WriteProcessMemory((IntPtr)this.sa2!, allocMemAddress, Encoding.Default.GetBytes(dllPath), (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), out _);
		if (!success) {
			this.HandleInjectionFailure();
		}

		IntPtr helperThread = CreateRemoteThread((IntPtr)this.sa2!, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
		if (helperThread == IntPtr.Zero) {
			this.HandleInjectionFailure();
		}

		WaitForSingleObject(helperThread, WAIT_INFINITE);

		if (!GetExitCodeThread(helperThread, out uint helperExitCode)) {
			this.HandleMemoryError("Failed to inject helper to SA2 process.");
		}

		if (helperExitCode == 0) {
			this.HandleMemoryError("Failed to inject helper to SA2 process.");
		}
	}

	[DoesNotReturn]
	private void HandleInjectionFailure() {
		int errorCode = Marshal.GetLastPInvokeError();
		this.CloseInjectionResources();
		if (errorCode == ERROR_ACCESS_DENIED) {
			throw new SA2ProcessAccessException(GetInjectionFailureMessage(errorCode));
		}

		throw new SA2InjectionException(GetInjectionFailureMessage(errorCode));
	}

	private static string GetInjectionFailureMessage(int errorCode) {
		if (errorCode == ERROR_ACCESS_DENIED) {
			return "SA2 appears to be running elevated; restart SA2 normally or run this tool as administrator.";
		}

		return "Failed to inject helper to SA2 process.";
	}

	private void CloseInjectionResources() {
		if (SA2Manager.MemoryMapper != null) {
			SA2Manager.MemoryMapper.Dispose();
			SA2Manager.MemoryMapper = null;
		}

		this.CloseResource();
	}

	~SA2Manager() {
		this.CloseResource();
	}

	public static void Start(LevelRow selection, byte repetitions, HuntingTeacherForm teacherForm, bool repetitionsInPlace) {
		SA2Manager.CanRun = true;

		using (SA2Manager instance = new(selection, repetitions, teacherForm, repetitionsInPlace)) {
			while (SA2Manager.CanRun && !instance.level.SequenceComplete() && !instance.targetProcess.HasExited) {
				instance.sharedMemory.Read(0, out instance.HunterTeacherData);
				instance.level.RunSequence();
				instance.sharedMemory.Write(0, ref instance.HunterTeacherData);
			}

			instance.HunterTeacherData.currentLevel = 0;
			instance.HunterTeacherData.sequenceComplete = false;
			instance.sharedMemory.Write(0, ref instance.HunterTeacherData);

			instance.LogMessage("Sequence Complete!" + Environment.NewLine);

			if (instance.targetProcess.HasExited) {
				instance.LogMessage("SA2 Process Terminated - Resetting State.");
			}
		}

		teacherForm.Invoke(() => {
			teacherForm.ResetBtn_Click(teacherForm, new EventArgs());
		});
	}

	public static void Stop() {
		SA2Manager.CanRun = false;
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

	[LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial IntPtr GetModuleHandle(string lpModuleName);

	[LibraryImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
	private static partial IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

	[LibraryImport("kernel32.dll")]
	private static partial IntPtr WaitForSingleObject(IntPtr hThread, uint dwMilliseconds);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

	[LibraryImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesRead);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesWritten);

	[LibraryImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool CloseHandle(IntPtr hProcess);

	// used for memory allocation
	private const uint MEM_COMMIT = 0x00001000;
	private const uint MEM_RESERVE = 0x00002000;
	private const uint PAGE_READWRITE = 4;
	private const uint WAIT_INFINITE = 0xFFFFFFFF;
	private const int ERROR_ACCESS_DENIED = 5;
}

internal class SA2InjectionException(string message) : Exception(message);

internal class SA2ProcessAccessException(string message) : SA2InjectionException(message);

[Flags]
internal enum ProcessAccessFlags : uint {
	VMRead = 0x0010,
	VMWrite = 0x0020,
	VMOperation = 0x0008,
	QueryInformation = 0x0400,
	CreateThread = 0x0002,
	All = 0x001F0FFF
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct HunterTeacherData {
	public int currentLevel;
	public bool inWinScreen;
	public bool sequenceComplete;
	public bool levelLoading;
	public bool mspReversedHints;
	public bool backToMenu;
	public bool timerReset;
	public int p1Id;
	public int p2Id;
	public int p3Id;
}
