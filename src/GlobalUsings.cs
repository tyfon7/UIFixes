// These shouldn't change (unless they do)
global using GridItemAddress = ItemAddressClass;
global using DragItemContext = ItemContextClass;
global using InsuranceItem = ItemClass;

// Everything below will change between EFT builds
global using BaseItemInfoInteractions = GClass3042;
global using GenericItemContext = GClass2833;
global using KeyCombination = GClass1911;
global using ToggleKeyCombination = GClass1912;
global using Stackable = GClass2751;
global using RagfairSearch = GClass3219;
global using CurrencyInfo = GClass2531;
global using Scheme = GClass1939;
global using ItemFilterExtensions = GClass2524;
global using QuickBindCommandMap = GClass3032;
global using DiscardResult = GClass2799;
global using ItemSorter = GClass2772;
global using ItemWithLocation = GClass2521;
global using SearchableGrid = GClass2516;
global using CursorManager = GClass3034;
global using Helmet = GClass2651;

// State machine states
global using FirearmReadyState = EFT.Player.FirearmController.GClass1619;
global using FirearmAddingModState = EFT.Player.FirearmController.Class1039;

// JSON
global using LocationJsonParser = GClass1496;
global using JsonItem = GClass1198;

// Errors
global using DestroyError = GClass3344;
global using GridNoRoomError = StashGridClass.GClass3310;
global using GridSpaceTakenError = StashGridClass.GClass3311;
global using GridModificationsUnavailableError = StashGridClass.GClass3315;
global using NoRoomError = GClass3316;
global using NoPossibleActionsError = GClass3317;
global using CannotSortError = GClass3325;
global using FailedToSortError = GClass3326;
global using MoveSameSpaceError = InteractionsHandlerClass.GClass3353;
global using NotModdableInRaidError = GClass3321;
global using MultitoolNeededError = GClass3322;
global using ModVitalPartInRaidError = GClass3323;
global using SlotNotEmptyError = EFT.InventoryLogic.Slot.GClass3339;

// Operations
global using ItemOperation = GStruct413;
global using MoveOperation = GClass2802;
global using AddOperation = GClass2798;
global using ResizeOperation = GClass2803;
global using FoldOperation = GClass2815;
global using NoOpMove = GClass2795;
global using BindOperation = GClass2818;
global using SortOperation = GClass2824;
global using TargetItemOperation = TraderControllerClass.Struct775;

// Interfaces
global using IApplicable = GInterface321;
