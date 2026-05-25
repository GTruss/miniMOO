using miniMOO.Core.Things;

namespace miniMOO.Host.World;

public static class WorldIds {
    public static readonly ObjectId Root = new(1);
    public static readonly ObjectId Wizard = new(2);
    public static readonly ObjectId Room = new(3);
    public static readonly ObjectId Builder = new(4);
    public static readonly ObjectId Thing = new(5);
    public static readonly ObjectId Player = new(6);
    public static readonly ObjectId Exit = new(7);
    public static readonly ObjectId Container = new(8);
    public static readonly ObjectId Note = new(9);
    public static readonly ObjectId PlayerStart = new(10);

    public static readonly ObjectId StringUtils   = new(20);
    public static readonly ObjectId BuildingUtils = new(21);
    public static readonly ObjectId GenderUtils = new(41);
    public static readonly ObjectId ObjectUtils = new(52);
    public static readonly ObjectId ListUtils = new(55);
    public static readonly ObjectId CommandUtils = new(56);
    public static readonly ObjectId CodeUtils = new(59);
    public static readonly ObjectId SeqUtils = new(33);

    public static readonly ObjectId MailPlayer = new(40);
    public static readonly ObjectId Wiz = new(57);
    public static readonly ObjectId Prog = new(58);
    public static readonly ObjectId FrandsPlayerClass = new(88);

    public static readonly ObjectId GenericEditor = new(50);
    public static readonly ObjectId VerbEditor = new(49);
    
    public static readonly ObjectId GenericOptionPackage = new(68);
    public static readonly ObjectId GenericUtilitiesPackage = new(78);

    public static readonly ObjectId DisplayOptions = new(67);
    public static readonly ObjectId BuilderOptions = new(77);
    public static readonly ObjectId ProgrammerOptions = new(76);

    public static readonly ObjectId Foyer = new(101);
    public static readonly ObjectId Library = new(102);
    public static readonly ObjectId Staff = new(103);
    public static readonly ObjectId ExitEast = new(104);
    public static readonly ObjectId ExitWest = new(105);
    public static readonly ObjectId WornBook = new(106);
}
