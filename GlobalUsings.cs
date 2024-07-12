// These shouln't change (unless they do)
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

// Errors
global using DestroyError = GClass3344;
global using GridModificationsUnavailableError = StashGridClass.GClass3315;
global using NoRoomError = GClass3316;
global using NoPossibleActionsError = GClass3317;
global using MoveSameSpaceError = InteractionsHandlerClass.GClass3353;

// Operations
global using ItemOperation = GStruct413;
global using MoveOperation = GClass2802;
global using NoOpMove = GClass2795;
global using TargetItemOperation = TraderControllerClass.Struct775;

// Interfaces
global using IApplicable = GInterface321;