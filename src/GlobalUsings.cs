// These shouldn't change (unless they do) (they did)
global using DragItemContext = ItemContextClass;
global using InsuranceItem = ItemClass;
global using ToggleKeyCombination = DeleteNoteDescriptorClass; // automatic name detection isn't without its downsides...

// Everything below will change between EFT builds
global using ItemProperties = GClass825;
global using GridItemAddress = GClass3186;
global using StashGridItemAddress = StashGridClass.Class2343;
global using BaseItemInfoInteractions = GClass3468;
global using GenericItemContext = GClass3243;
global using ModdingItemContext = GClass3246;
global using KeyCombination = GClass2172;
global using RagfairSearch = GClass3653;
global using CurrencyInfo = GClass2934;
global using Scheme = GClass2202;
global using ItemFilterExtensions = GClass2928;
global using CursorManager = GClass3460;
global using ContainerCollection = GClass3050;
global using BuildItemSelector = GClass3257;
global using ModdingItemSelector = GClass3258;
global using RaidPlayer = GClass1341;
global using AnimatorWrapper = GClass1375;
global using NaiveAcceptable = GClass3550;
global using FirearmInputHandler = Class1604;

// Context menus
global using InventoryInteractions = GClass3471; // There are two child versions?
global using TradingPlayerInteractions = GClass3481;
global using TransferPlayerInteractions = GClass3484;

// State machine states
global using FirearmReadyState = EFT.Player.FirearmController.GClass1806;
global using FirearmAddingModState = EFT.Player.FirearmController.Class1145;
global using FirearmInsertedMagState = EFT.Player.FirearmController.GClass1808;

// JSON
global using LocationJsonParser = GClass1682;
global using JsonItem = GClass1319;

// Errors
global using GenericError = GClass3854;
global using EmptyError = GClass3856;
global using DestroyError = GClass3823;
global using GridNoRoomError = StashGridClass.GClass3782;
global using GridSpaceTakenError = StashGridClass.GClass3783;
global using GridModificationsUnavailableError = StashGridClass.GClass3787;
global using NoRoomError = GClass3789;
global using NoPossibleActionsError = GClass3790;
global using MoveSameSpaceError = InteractionsHandlerClass.GClass3832;
global using NotApplicableError = GClass3793;
global using NotModdableInRaidError = GClass3794;
global using MultitoolNeededError = GClass3795;
global using ModVitalPartInRaidError = GClass3796;
global using CannotApplyError = GClass3797;
global using UnsearchedContainerError = GClass3805;
global using SlotNotEmptyError = EFT.InventoryLogic.Slot.GClass3818;
global using InvalidMagPresetError = MagazineBuildClass.Class3460;

// Operations
global using ItemOperation = GStruct454;
global using MoveOperation = GClass3203;
global using AddOperation = GClass3197;
global using RemoveOperation = GClass3202;
global using ResizeOperation = GClass3208;
global using SwapOperation = GClass3218;
global using FoldOperation = GClass3220;
global using NoOpMove = GClass3194;
global using DiscardOperation = GClass3200;
global using BindOperation = GClass3223;
global using UnbindOperation = GClass3224;
global using TargetItemOperation = TraderControllerClass.Struct865;

// Interfaces
global using IInputKey = GInterface213;
global using IResizeResult = GInterface381;
global using IApplicable = GInterface382;
global using IEncodable = GInterface386;
global using IItemResult = GInterface398;
