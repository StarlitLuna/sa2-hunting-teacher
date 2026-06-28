#pragma once

#include <stdint.h>
#include <Windows.h>

/************************/
/*  Constants           */
/************************/
/****** API Version ******************************************************************************/
/*
*   Description:
*     Current version of the save-state API table. Future versions append fields to
*     DebugModeSaveApi. Existing fields must not be reordered or removed.
*/
#define DEBUGMODE_SAVE_API_VERSION (1u)

/****** Capability Flags *************************************************************************/
/*
*   Description:
*     Flags returned by DebugModeSaveApi::capabilities. Check these before using a
*     category of functionality when the exact API version is not important.
*/
#define DEBUGMODE_SAVE_API_CAPABILITY_HOOKS             (0x00000001u)
#define DEBUGMODE_SAVE_API_CAPABILITY_PENDING_ACCESSORS (0x00000002u)
#define DEBUGMODE_SAVE_API_CAPABILITY_RESULT_CODES      (0x00000004u)

/****** Event Flags ******************************************************************************/
/*
*   Description:
*     Convert a DebugModeSaveEvent value into the matching bit for DebugModeSaveApi::supportedEvents.
*/
#define DEBUGMODE_SAVE_EVENT_BIT(eventValue) (1u << ((uint32_t)(eventValue)))

/************************/
/*  Enums               */
/************************/
/****** Save Events ******************************************************************************/
/*
*   Description:
*     Save-state lifecycle events. Hooks register for one event at a time. A mod
*     that needs multiple lifecycle points should register one hook per event.
*/
typedef enum DebugModeSaveEvent {
	/* Slot data is about to be captured. Debug Mode has already accepted the save request after validation. */
	DebugModeSaveEvent_BeforeSave = 0,
	/* Slot data has been captured and saved successfully to the save state. */
	DebugModeSaveEvent_AfterSave = 1,
	/* A valid load request is about to run Debug Mode's internal level restart. */
	DebugModeSaveEvent_BeforeLoadRestart = 2,
	/* Static slot state has been staged and the level manager has not loaded yet. */
	DebugModeSaveEvent_BeforeLevelSetup = 3,
	/* Game info and SET-backed state have been restored. Player state may still fail after this. */
	DebugModeSaveEvent_AfterGameRestore = 4,
	/* Player state has been restored and delayed restore work has been queued. */
	DebugModeSaveEvent_AfterPlayerRestore = 5,
	/* All delayed restore work has finished. */
	DebugModeSaveEvent_AfterDelayedRestore = 6,
	/* A queued load failed and Debug Mode is about to clear pending load state. */
	DebugModeSaveEvent_LoadFailed = 7,
	/* All Debug Mode save slots have been cleared. DebugModeSaveContext::slot is 0 for this event. */
	DebugModeSaveEvent_SlotsReset = 8
} DebugModeSaveEvent;

/****** API Results ******************************************************************************/
/*
*   Description:
*     Return values for hook registration calls.
*/
typedef enum DebugModeSaveResult {
	DebugModeSaveResult_Ok = 0,
	DebugModeSaveResult_InvalidArgument = 1,
	DebugModeSaveResult_UnsupportedEvent = 2,
	DebugModeSaveResult_HookNotFound = 3,
	DebugModeSaveResult_HookIdUnavailable = 4
} DebugModeSaveResult;

/************************/
/*  Types               */
/************************/
/****** Callback Context *************************************************************************/
/*
*   Description:
*     Data passed to save-state hook callbacks.
*
*   Notes:
*     - Check 'size' before reading fields added by future API versions.
*     - 'slot' is the Debug Mode slot affected by the current event. For
*         DebugModeSaveEvent_SlotsReset, every slot was cleared and 'slot' is 0.
*/
typedef struct DebugModeSaveContext {
	uint32_t version; /* DEBUGMODE_SAVE_API_VERSION for this callback context. */
	uint32_t size;    /* Size of this structure in bytes. */
	uint32_t slot;    /* Save-state slot index for the current event. */
	uint32_t level;   /* Current SA2 level ID when the event fired. */
} DebugModeSaveContext;

/****** Callback *********************************************************************************/
/*
*   Description:
*     Hook callback invoked by Debug Mode during a save-state lifecycle event.
*
*   Parameters:
*     - context  : Event context. Valid only for the duration of the callback.
*     - userData : Caller-owned pointer supplied to RegisterHook.
*/
typedef void(__cdecl* DebugModeSaveCallback)(const DebugModeSaveContext* context, void* userData);

/****** API Table ********************************************************************************/
/*
*   Description:
*     Save-state API table returned by DebugMode_GetSaveApi.
*
*   Version History:
*     - Version 1: Initial API with hook registration and pending-state accessors.
*
*   Notes:
*     - Check 'version' and 'size' before using fields from future versions.
*     - Function pointers remain valid for the lifetime of the process.
*/
typedef struct DebugModeSaveApi {
	uint32_t version;         /* API table version. */
	uint32_t size;            /* Size of this structure in bytes. */
	uint32_t capabilities;    /* DEBUGMODE_SAVE_API_CAPABILITY_* bitmask. */
	uint32_t supportedEvents; /* DEBUGMODE_SAVE_EVENT_BIT(DebugModeSaveEvent_*) bitmask. */
	uint32_t slotCount;       /* Number of Debug Mode save-state slots. Valid slot indexes are 0 to slotCount - 1. */

	/****** Version >= 1 ************************************************************************/

	/**** Hook Registry *************************************/
	/*
	*   Description:
	*     Register one hook for one save-state lifecycle event.
	*
	*   Parameters:
	*     - event    : Event to receive.
	*     - callback : Function to call when the event fires.
	*     - userData : Caller-owned pointer passed back to callback.
	*     - hookId   : Receives the generated hook ID required to unregister this hook.
	*
	*   Returns:
	*     DebugModeSaveResult_Ok on success, or a failure result describing why the
	*     hook was not registered. The hookId value is meaningful only when this
	*     returns DebugModeSaveResult_Ok; generated hook IDs are always nonzero.
	*/
	DebugModeSaveResult(__cdecl* RegisterHook)(DebugModeSaveEvent event, DebugModeSaveCallback callback, void* userData, uint64_t* hookId);

	/*
	*   Description:
	*     Unregister a hook previously registered through RegisterHook.
	*
	*   Parameters:
	*     - hookId : Hook ID returned by RegisterHook.
	*
	*   Returns:
	*     DebugModeSaveResult_Ok on success, or DebugModeSaveResult_HookNotFound if
	*     no live hook exists for hookId.
	*/
	DebugModeSaveResult(__cdecl* UnregisterHook)(uint64_t hookId);

	/**** State Query ***************************************/
	/*
	*   Description:
	*     If Debug Mode save states are enabled in configuration.
	*/
	BOOL(__cdecl* IsSaveStateEnabled)(void);

	/*
	*   Description:
	*     If a save-state load has been requested but has not completed.
	*/
	BOOL(__cdecl* HasPendingSaveLoad)(void);

	/*
	*   Description:
	*     If Debug Mode is currently processing its save-state load path.
	*/
	BOOL(__cdecl* IsSaveLoadInProgress)(void);

	/*
	*   Description:
	*     If delayed ground restore work is still pending.
	*/
	BOOL(__cdecl* HasPendingGroundRestore)(void);

	/*
	*   Description:
	*     If delayed camera restore work is still pending.
	*/
	BOOL(__cdecl* HasPendingCameraRestore)(void);

	/*
	*   Description:
	*     If delayed hunting emerald manager restore work is still pending.
	*/
	BOOL(__cdecl* HasPendingHuntingRestore)(void);

	/*
	*   Description:
	*     If any delayed restore work is still pending.
	*/
	BOOL(__cdecl* HasPendingDelayedRestore)(void);
} DebugModeSaveApi;

/****** API Getter *******************************************************************************/
/*
*   Description:
*     Function pointer type for resolving DebugMode_GetSaveApi through GetProcAddress.
*/
typedef const DebugModeSaveApi* (__cdecl* DebugModeGetSaveApi)(void);
