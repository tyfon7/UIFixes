// These shouldn't change (unless they do) (they did)
//global using DragItemContext = ItemContextClass;
//global using InsuredItem = ItemClass;
//global using ToggleInputKeyCombination = DeleteNoteDescriptorClass; // automatic name detection isn't without its downsides...

// Everything below will change between EFT builds
//global using UnparsedData = GClass846;
//global using GridItemAddress = GClass3393;
//global using SlotItemAddress = GClass3391;
//global using Grid.ProtectedGridItemAddress = Grid.Class2458;
//global using DefaultItemContext = GClass3453;
//global using ModdingSelectableItemContext = GClass3456;
//global using RagfairSearch = GClass3943;
//global using CurrencyUtil = GClass3130;
//global using ProductionScheme = GClass2440;
//global using ItemFilterExtension = GClass3124;
//global using CursorSwitcher = GClass3746;
//global using ContainerCollection = GClass3248;
//global using EditBuildManipulation = GClass3468;
//global using WeaponModdingManipulation = GClass3469;
//global using UnityAnimatorWrapper = GClass1446;
//global using EditBuildNameWindowContext = GClass3838;
//global using FirearmHandsInputTranslator = Class1730;
//global using TradingScreenController = EFT.UI.TraderScreensGroup.GClass3888;
//global using DailyQuest = GClass3996;
//global using HandbookNodes = GClass1625;
//global using ArmorSlot = GClass3125;
//global using ArmorPlatesFormatter = GClass3132;
//global using WishlistManager = GClass2067;
//global using ProfileUpdatesHandler = GClass2331;
//global using InputKey = GClass2412;
//global using WeaponAssembler = GClass3470;
//global using LeftHandController = GClass2725;
//global using QuestControllerClient = GClass4005;
//global using WindowContext = GClass3829;
//global using ItemUiContext.WindowData = EFT.UI.ItemUiContext.Class2918;
//global using QuickUseSelectorUtil = GClass3744;
//global using EftBattleUIScreen.HideoutBattleUIScreenController = EFT.UI.EftBattleUIScreen.GClass3867;

// Context menus
//global using BaseInventoryItemContextInteractions = GClass3757; // There are two child versions?
//global using TradingPlayerContextInteractions = GClass3767;
//global using TransferItemPlayerContextInteractions = GClass3770;
//global using ModdingContextInteractions = GClass3762;
//global using WishlistContextInteractions = GClass3782;
//global using LoadMagContextInteractions = GClass3779;

// State machine states
//global using FirearmController.Idling = EFT.Player.FirearmController.GClass2037;
//global using FirearmController.AddModOperation = EFT.Player.FirearmController.Class1264;
//global using FirearmController.InsertMagOperation = EFT.Player.FirearmController.GClass2039;
//global using FirearmController.ReloadExternalMagOperation = EFT.Player.FirearmController.GClass2016;

// JSON
//global using ItemDeserializer = GClass1911;

// Errors
//global using StringError = GClass1522;
//global using SkipError = GClass1523;
//global using DiscardLimitsReachedError = GClass1583;
//global using Grid.WontFitToGridError = Grid.GClass1542;
//global using Grid.PlaceTakenByAnotherItemError = Grid.GClass1543;
//global using Grid.ModificationUnavailable = Grid.GClass1547;
//global using NoFreeSpaceError = GClass1549;
//global using NoPossibleActionsError = GClass1550;
//global using ItemManipulator.ItemAlreadyThereError = ItemManipulator.GClass1592;
//global using ItemManipulator.ResizeError = ItemManipulator.GClass1605;
//global using NoPossibleActionsError = GClass1550;
//global using NotRaidModdableError = GClass1554;
//global using NotModdableWithoutMultitoolError = GClass1555;
//global using CannotModifyVitalInRaidError = GClass1556;
//global using CannotApplyItemError = GClass1557;
//global using UnknownAddressError = GClass1565;
//global using Slot.SlotNotEmptyError = EFT.InventoryLogic.Slot.GClass1578; 
//global using MagBuildsStorage.PresetSourceError = MagBuildsStorage.Class1022;
//global using None = GClass1616; // This is a weird thing that is the generic type of some returns (e.g. GStructXXX<None>) that is always overridden

// Operations
//global using OperationResult = GStruct153;
//global using MoveResult = GClass3411;
//global using AddResult = GClass3405;
//global using RemoveResult = GClass3410;
//global using ResizeResult = GClass3416;
//global using MergeResult = GClass3417;
//global using SwapResult = GClass3426;
//global using FoldResult = GClass3428;
//global using IgnoreDiscardLimitsResult = GClass3402;
//global using DiscardResult = GClass3408;
//global using BindResult = GClass3431;
//global using UnbindResult = GClass3432;
//global using SetPinLockResult = GClass3534;
//global using TargetItemOperation = EFT.InventoryLogic.ItemController.Struct897;

// Interfaces
//global using IGameKey = GInterface240;
//global using IApplicable = GInterface408;
//global using IRecodableItem = GInterface412;
//global using IItemOperationResult = GInterface424;
//global using IPossibleDestroyResult = GInterface427;
//global using ITransferOrMergeResult = GInterface429;
