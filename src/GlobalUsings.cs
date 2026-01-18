// These shouldn't change (unless they do) (they did)
global using DragItemContext = ItemContextClass;
global using InsuranceItem = ItemClass;
global using ToggleKeyCombination = DeleteNoteDescriptorClass; // automatic name detection isn't without its downsides...

// Everything below will change between EFT builds
global using ItemProperties = GClass846;
global using GridItemAddress = GClass3393;
global using StashGridItemAddress = StashGridClass.Class2458;
global using GenericItemContext = GClass3453;
global using ModdingItemContext = GClass3456;
global using RagfairSearch = GClass3943;
global using CurrencyInfo = GClass3130;
global using Scheme = GClass2440;
global using ItemFilterExtensions = GClass3124;
global using CursorManager = GClass3746;
global using ContainerCollection = GClass3248;
global using BuildItemSelector = GClass3468;
global using ModdingItemSelector = GClass3469;
global using AnimatorWrapper = GClass1446;
global using NaiveAcceptable = GClass3838;
global using FirearmInputHandler = Class1730;
global using TradingScreenController = EFT.UI.TraderScreensGroup.GClass3888;
global using DailyQuest = GClass3996;
global using CurrentyHelper = GClass3130;
global using EntityNodeDictionary = GClass1625;
global using ArmorSlot = GClass3125;
global using ArmorFormatter = GClass3132;
global using WishlistManager = GClass2067;
global using ItemReceiver = GClass2331;
global using KeyPressState = GClass2412;
global using FirearmHandsInputTranslator = Class1730;
global using WeaponBuilder = GClass3470;
global using LeftHandController = GClass2725;
global using QuestController = GClass4005;
global using WindowContext = GClass3829;
global using WindowData = EFT.UI.ItemUiContext.Class2918;

// Context menus
global using InventoryInteractions = GClass3757; // There are two child versions?
global using TradingPlayerInteractions = GClass3767;
global using TransferPlayerInteractions = GClass3770;
global using ModdingItemInteractions = GClass3762;
global using WishListInteractions = GClass3782;
global using LoadAmmoInteractions = GClass3779;

// State machine states
global using FirearmReadyState = EFT.Player.FirearmController.GClass2037;
global using FirearmAddingModState = EFT.Player.FirearmController.Class1264;
global using FirearmInsertedMagState = EFT.Player.FirearmController.GClass2039;

// JSON
global using LocationJsonParser = GClass1911;

// Errors
global using GenericError = GClass1522;
global using EmptyError = GClass1523;
global using DestroyError = GClass1583;
global using GridNoRoomError = StashGridClass.GClass1542;
global using GridSpaceTakenError = StashGridClass.GClass1543;
global using GridModificationsUnavailableError = StashGridClass.GClass1547;
global using NoRoomError = GClass1549;
global using NoPossibleActionsError = GClass1550;
global using MoveSameSpaceError = InteractionsHandlerClass.GClass1592;
global using MoveResizeError = InteractionsHandlerClass.GClass1605;
global using NotApplicableError = GClass1550;
global using NotExaminedError = GClass1551;
global using NotModdableInRaidError = GClass1554;
global using MultitoolNeededError = GClass1555;
global using ModVitalPartInRaidError = GClass1556;
global using CannotApplyError = GClass1557;
global using UnsearchedContainerError = GClass1565;
global using SlotNotEmptyError = EFT.InventoryLogic.Slot.GClass1578; 
global using InvalidMagPresetError = MagazineBuildClass.Class1022;
global using UnsetError = GClass1616; // This is a weird thing that is the generic type of some returns (e.g. GStructXXX<UnsetError>) that is always overridden

// Operations
global using ItemOperation = GStruct153;
global using MoveOperation = GClass3411;
global using AddOperation = GClass3405;
global using RemoveOperation = GClass3410;
global using ResizeOperation = GClass3416;
global using MergeOperation = GClass3417;
global using SwapOperation = GClass3426;
global using FoldOperation = GClass3428;
global using NoOpMove = GClass3402;
global using DiscardOperation = GClass3408;
global using TransferOperation = GClass3425;
global using BindOperation = GClass3431;
global using UnbindOperation = GClass3432;
global using TargetItemOperation = TraderControllerClass.Struct897;

// Interfaces
global using IInputKey = GInterface240;
global using IResizeResult = GInterface407;
global using IApplicable = GInterface408;
global using IEncodable = GInterface412;
global using IItemResult = GInterface424;
global using ITargetItemResult = GInterface429;
