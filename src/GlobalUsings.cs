// These shouldn't change (unless they do) (they did)
global using DragItemContext = ItemContextClass;
global using InsuranceItem = ItemClass;
global using ToggleKeyCombination = DeleteNoteDescriptorClass; // automatic name detection isn't without its downsides...

// Everything below will change between EFT builds
global using ItemProperties = GClass846;  // GClass825;
global using GridItemAddress = GClass3393; //  GClass3186;
global using StashGridItemAddress = StashGridClass.Class2458; // StashGridClass.Class2343;
global using BaseItemInfoInteractions = ContextInteractionsAbstractClass; // GClass3468;
global using GenericItemContext = GClass3453; // GClass3243;
global using ModdingItemContext = GClass3456; // GClass3246;
global using KeyCombination = KeyBindingClass; // GClass2172;
global using RagfairSearch = GClass3943; // GClass3653;
global using CurrencyInfo = GClass3130; // GClass2934;
global using Scheme = GClass2440; // GClass2202;
global using ItemFilterExtensions = GClass3124; // GClass2928;
global using CursorManager = GClass3746; // GClass3460;
global using ContainerCollection = GClass3248; // GClass3050;
global using BuildItemSelector = GClass3468; // GClass3257;
global using ModdingItemSelector = GClass3469; // GClass3258;
global using RaidPlayer = GroupPlayerViewModelClass; // GClass1341;
global using AnimatorWrapper = GClass1446; // GClass1375;
global using NaiveAcceptable = GClass3838; // GClass3550;
global using FirearmInputHandler = Class1730; // Class1604;
global using TradingScreenController = EFT.UI.TraderScreensGroup.GClass3888; // EFT.UI.TraderScreensGroup.GClass3599;
global using DailyQuest = GClass3996; // GClass3691;
global using CurrentyHelper = GClass3130; // GClass2934;
global using EntityNodeDictionary = GClass1625; // GClass3865;
global using ArmorSlot = GClass3125; // GClass2929;
global using ArmorFormatter = GClass3132; // GClass2936;
global using WishlistManager = GClass2067; // GClass1836;

// Context menus
global using InventoryInteractions = GClass3757; // GClass3471; // There are two child versions?
global using TradingPlayerInteractions = GClass3767; // GClass3481;
global using TransferPlayerInteractions = GClass3768; // GClass3484;
global using ModdingItemInteractions = GClass3762; // GClass3476;
global using WishListInteractions = GClass3782; // GClass3496;

// State machine states
global using FirearmReadyState = EFT.Player.FirearmController.GClass2037; // EFT.Player.FirearmController.GClass1806;
global using FirearmAddingModState = EFT.Player.FirearmController.Class1264; // EFT.Player.FirearmController.Class1145;
global using FirearmInsertedMagState = EFT.Player.FirearmController.GClass2039; // EFT.Player.FirearmController.GClass1808;

// JSON
global using LocationJsonParser = GClass1911; // GClass1682;
global using JsonItem = FlatItemsDataClass; // GClass1319;

// Errors
global using GenericError = GClass1522; // GClass3854;
global using EmptyError = GClass1523; // GClass3856;
global using DestroyError = GClass1583; // GClass3823;
global using GridNoRoomError = StashGridClass.GClass1542; // StashGridClass.GClass3782;
global using GridSpaceTakenError = StashGridClass.GClass1543; // StashGridClass.GClass3783;
global using GridModificationsUnavailableError = StashGridClass.GClass1547; // StashGridClass.GClass3787;
global using NoRoomError = GClass1549; // GClass3789;
global using NoPossibleActionsError = GClass1550; // GClass3790;
global using MoveSameSpaceError = InteractionsHandlerClass.GClass1592; // InteractionsHandlerClass.GClass3832;
global using NotApplicableError = GClass1550; // GClass3793;
global using NotModdableInRaidError = GClass1554; // GClass3794;
global using MultitoolNeededError = GClass1555; // GClass3795;
global using ModVitalPartInRaidError = GClass1556; // GClass3796;
global using CannotApplyError = GClass1557; // GClass3797;
global using UnsearchedContainerError = GClass1565; // GClass3805;
global using SlotNotEmptyError = EFT.InventoryLogic.Slot.GClass1578; // EFT.InventoryLogic.Slot.GClass3818;
global using InvalidMagPresetError = MagazineBuildClass.Class1022; // MagazineBuildClass.Class3460;

// Operations
global using ItemOperation = GStruct153; // GStruct454;
global using MoveOperation = GClass3411; // GClass3203;
global using AddOperation = GClass3405; // GClass3197;
global using RemoveOperation = GClass3410; // GClass3202;
global using ResizeOperation = GClass3416; // GClass3208;
global using SwapOperation = GClass3426; // GClass3218;
global using FoldOperation = GClass3428; // GClass3220;
global using NoOpMove = GClass3402; // GClass3194;
global using DiscardOperation = GClass3408; // GClass3200;
global using BindOperation = GClass3431; // GClass3223;
global using UnbindOperation = GClass3432; // GClass3224;
global using TargetItemOperation = TraderControllerClass.Struct897; // TraderControllerClass.Struct865;

// Interfaces
global using IInputKey = GInterface240; // GInterface213;
global using IResizeResult = GInterface407; // GInterface381;
global using IApplicable = GInterface408; // GInterface382;
global using IEncodable = GInterface412; // GInterface386;
global using IItemResult = GInterface424; // GInterface398;
