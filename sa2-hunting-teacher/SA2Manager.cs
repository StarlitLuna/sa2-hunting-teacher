using sa2_hunting_teacher.Knuckles;
using sa2_hunting_teacher.Rouge;
using System.Diagnostics;
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
	private readonly bool repititionsInPlace;
	private HunterTeacherData HunterTeacherData;

	private SA2Manager(Level selection, byte repetitions, HuntingTeacherForm teacherForm, bool repititionsInPlace) {
		this.teacherForm = teacherForm;
		this.level = selection switch {
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

		SA2Manager.MemoryMapper ??= MemoryMappedFile.CreateOrOpen(
			"SA2-Hunter-Teacher",
			Marshal.SizeOf<HunterTeacherData>(),
			MemoryMappedFileAccess.ReadWrite
		);

		this.repititionsInPlace = repititionsInPlace;
		this.sharedMemory = SA2Manager.MemoryMapper.CreateViewAccessor();
		this.ApplyDataDefaults(this.level.LevelId, teacherForm.MspReversedHints(), teacherForm.BackToMenu());
		this.InjectDll();
	}

	private void ApplyDataDefaults(LevelId level, bool mspReversedHints, bool backToMenu) {
		this.HunterTeacherData.currentLevel = (int)level;
		this.HunterTeacherData.inWinScreen = false;
		this.HunterTeacherData.sequenceComplete = false;
		this.HunterTeacherData.mspReversedHints = mspReversedHints;
		this.HunterTeacherData.backToMenu = backToMenu;
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

	public bool RepititionsInPlace() {
		return this.repititionsInPlace;
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
		for (int i = 0; i < this.targetProcess.Modules.Count; i++) {
			if (this.targetProcess.Modules[i].ModuleName.ToLower().Equals(HELPER_DLL_NAME)) {
				return true;
			}
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
		bool success = WriteProcessMemory((IntPtr)this.sa2!, allocMemAddress, Encoding.Default.GetBytes(dllPath), (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), out _);
		if (!success) {
			this.HandleMemoryError("Failed to inject helper to SA2 process.");
			return;
		}

		IntPtr helperThread = CreateRemoteThread((IntPtr)this.sa2!, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
		WaitForSingleObject(helperThread, WAIT_INFINITE);

		if (!GetExitCodeThread(helperThread, out uint helperExitCode)) {
			this.HandleMemoryError("Failed to inject helper to SA2 process.");
		}

		if (helperExitCode == 0) {
			this.HandleMemoryError("Failed to inject helper to SA2 process.");
		}
	}

	~SA2Manager() {
		this.CloseResource();
	}

	public static void Start(Level selection, byte repetitions, HuntingTeacherForm teacherForm, bool repititionsInPlace) {
		SA2Manager.CanRun = true;
		 
		using (SA2Manager instance = new(selection, repetitions, teacherForm, repititionsInPlace)) {
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

	[LibraryImport("kernel32.dll")]
	private static partial IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

	[LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial IntPtr GetModuleHandle(string lpModuleName);
	
	[LibraryImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
	private static partial IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

	[LibraryImport("kernel32.dll")]
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
}

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
	public int p1Id;
	public int p2Id;
	public int p3Id;
}
