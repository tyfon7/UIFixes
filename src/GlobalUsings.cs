// These shouldn't change (unless they do) (they did)
global using DragItemContext = ItemContextClass;
global using InsuranceItem = ItemClass;
global using ToggleKeyCombination = DeleteNoteDescriptorClass; // automatic name detection isn't without its downsides...

// Everything below will change between EFT builds
global using GridItemAddress = GClass3115;
global using StashGridItemAddress = StashGridClass.Class2295;
global using BaseItemInfoInteractions = GClass3402;
global using GenericItemContext = GClass3172;
global using KeyCombination = GClass2129;
global using RagfairSearch = GClass3597;
global using CurrencyInfo = GClass2867;
global using Scheme = GClass2158;
global using ItemFilterExtensions = GClass2861;
global using CursorManager = GClass3394;
global using ContainerCollection = GClass2981;
global using BuildItemSelector = GClass3186;
global using ModdingItemSelector = GClass3187;

// Context menus
global using InventoryInteractions = GClass3405; // There are two child versions?
global using TradingPlayerInteractions = GClass3415;
global using TransferPlayerInteractions = GClass3418;

// State machine states
global using FirearmReadyState = EFT.Player.FirearmController.GClass1771;
global using FirearmAddingModState = EFT.Player.FirearmController.Class1131;

// JSON
global using LocationJsonParser = GClass1648;
global using JsonItem = GClass1301;

// Errors
global using GenericError = GClass3757;
global using EmptyError = GClass3759;
global using DestroyError = GClass3726;
global using GridNoRoomError = StashGridClass.GClass3686;
global using GridSpaceTakenError = StashGridClass.GClass3687;
global using GridModificationsUnavailableError = StashGridClass.GClass3691;
global using NoRoomError = GClass3693;
global using NoPossibleActionsError = GClass3694;
global using MoveSameSpaceError = InteractionsHandlerClass.GClass3735;
global using NotApplicableError = GClass3697;
global using NotModdableInRaidError = GClass3698;
global using MultitoolNeededError = GClass3699;
global using ModVitalPartInRaidError = GClass3700;
global using CannotApplyError = GClass3701;
global using UnsearchedContainerError = GClass3709;
global using SlotNotEmptyError = EFT.InventoryLogic.Slot.GClass3722;
global using InvalidMagPresetError = MagazineBuildClass.Class3388;

// Operations
global using ItemOperation = GStruct445;
global using MoveOperation = GClass3132;
global using AddOperation = GClass3126;
global using RemoveOperation = GClass3131;
global using ResizeOperation = GClass3137;
global using FoldOperation = GClass3149;
global using NoOpMove = GClass3123;
global using DiscardOperation = GClass3129;
global using BindOperation = GClass3152;
global using UnbindOperation = GClass3153;
global using TargetItemOperation = TraderControllerClass.Struct839;

// Interfaces
global using IApplicable = GInterface369;
