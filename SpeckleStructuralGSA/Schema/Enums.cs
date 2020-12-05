﻿namespace SpeckleStructuralGSA.Schema
{
  //These should be without version numbers
  public enum GwaKeyword
  {
    [StringValue("LOAD_NODE")]
    LOAD_NODE,
    [StringValue("NODE")]
    NODE,
    [StringValue("AXIS")]
    AXIS,
    [StringValue("LOAD_TITLE")]
    LOAD_TITLE,
    [StringValue("LOAD_GRID_AREA")]
    LOAD_GRID_AREA,
    [StringValue("GRID_SURFACE")]
    GRID_SURFACE,
    [StringValue("GRID_PLANE")]
    GRID_PLANE,
    [StringValue("EL")]
    EL,
    [StringValue("MEMB")]
    MEMB,
    [StringValue("LOAD_BEAM")]
    LOAD_BEAM,
    [StringValue("LOAD_BEAM_POINT")]
    LOAD_BEAM_POINT,
    [StringValue("LOAD_BEAM_UDL")]
    LOAD_BEAM_UDL,
    [StringValue("LOAD_BEAM_LINE")]
    LOAD_BEAM_LINE,
    [StringValue("LOAD_BEAM_PATCH")]
    LOAD_BEAM_PATCH,
    [StringValue("LOAD_BEAM_TRILIN")]
    LOAD_BEAM_TRILIN,
    [StringValue("ASSEMBLY")]
    ASSEMBLY
  }

  public enum Restraint
  {
    [StringValue("Fixed")]
    Fixed,
    [StringValue("Pinned")]
    Pinned,
    [StringValue("Full rotational")]
    FullRotational,
    [StringValue("Partial rotational")]
    PartialRotational,
    [StringValue("Top flange lateral")]
    TopFlangeLateral
  }

  public enum CurveType
  {
    NotSet = 0,
    Lagrange,
    Circular
  }

  public enum PointDefinition
  {
    NotSet = 0,
    Points,
    Spacing,
    Storey,
    Explicit
  }

  public enum GridExpansion
  {
    NotSet = 0,
    Legacy = 1,
    PlaneAspect = 2,
    PlaneSmooth = 3,
    PlaneCorner = 4
  }

  public enum GridSurfaceSpan
  {
    NotSet = 0,
    One = 1,
    Two = 2
  }

  public enum GridSurfaceElementsType
  {
    NotSet = 0,
    OneD = 1,
    TwoD = 2
  }

  //Note: these enum values map to different integers in GWA than is shown here
  public enum GridPlaneAxisRefType
  {
    NotSet = 0,
    Global,
    XElevation,
    YElevation,
    GlobalCylindrical,
    Reference
  }

  public enum GridPlaneType
  {
    NotSet = 0,
    General = 1,
    Storey = 2 
  }

  public enum AxisRefType
  {
    NotSet = 0,
    Global,
    Local,
    Reference
  }

  public enum LoadBeamAxisRefType
  {
    NotSet = 0,
    Global,
    Local,
    Natural,
    Reference
  }

  public enum AxisDirection3
  {
    NotSet = 0,
    X = 1,
    Y = 2,
    Z = 3,
  }

  public enum AxisDirection6
  {
    NotSet = 0,
    X,
    Y,
    Z,
    XX,
    YY,
    ZZ
  }

  public enum LoadAreaOption
  {
    NotSet = 0,
    Plane,
    PolyRef,
    Polygon
  }

  public enum StreamBucket
  {
    Model,
    Results
  }

  public enum MemberGeometry
  {
    NotSet = 0,
    LINEAR,
    ARC,
    RAD
  }

  public enum MeshingOption
  {
    NotSet = 0,
    MESH,
    SELF,
    NONE
  }

  //In keeping with the approach to be as close to the inherent GWA schema as possible, this is kept as one list, and not split up into one for 1D and one for 2D
  public enum AnalysisType
  {
    NotSet = 0,
    //1D items - when MEMB is set (using MemberType enum above) to one one of the 1D types, then, and only then, is one of the following 10 is valid
    BEAM,
    BAR,
    STRUT,
    TIE,
    ROD,
    LINK,
    SPRING,
    CABLE,
    SPACER,
    DAMPER,
    //2D items - when MEMB is set to be a 2D type, then, and only then, are one of the following valid
    EL_TYPE,
    LINEAR,
    QUADRATIC,
    RIGID
  }

  //The number values are key here
  public enum FireResistance
  {
    Undefined = 0,
    HalfHour = 30,
    OneHour = 60,
    OneAndAHalfHours = 90,
    TwoHours = 120,
    ThreeHours = 180,
    FourHours = 240
  }

  public enum LoadHeightReferencePoint
  {
    [StringValue("")]
    NotSet = 0,
    [StringValue("SHR_CENTRE")]
    ShearCentre,
    [StringValue("TOP_FLANGE")]
    TopFlange,
    [StringValue("BOT_FLANGE")]
    BottomFlange
  }

  public enum ReleaseCode
  {
    [StringValue("")]
    NotSet = 0,
    [StringValue("F")]
    Fixed,
    [StringValue("R")]
    Released,
    [StringValue("K")]
    Stiff
  }

  //Need StringValues here because the value to be inserted as a string into GWA starts with a number, so the idea of simply using <enum value>.ToString() doesn't work
  //Note: these values differ from the documentation for 10.1, which is incorrect; these values are taken from copying GWA commands from a 10.1 file
  public enum MemberType
  {
    [StringValue("")]
    NotSet = 0,
    //1D
    [StringValue("BEAM")]
    Beam,
    [StringValue("COLUMN")]
    Column,
    [StringValue("1D_GENERIC")]
    Generic1d,
    [StringValue("1D_VOID_CUTTER")]
    Void1d,
    //2D
    [StringValue("SLAB")]
    Slab,
    [StringValue("WALL")]
    Wall,
    [StringValue("2D_GENERIC")]
    Generic2d,
    [StringValue("2D_VOID_CUTTER")]
    Void2d,
    //3D
    [StringValue("3D_GENERIC")]
    Generic3d
  }

  public enum ExposedSurfaces
  {
    NotSet = 0,
    ALL,
    THREE,
    TOP_BOT,
    SIDES,
    ONE,
    NONE
  }

  public enum EffectiveLengthType
  {
    [StringValue("AUTOMATIC")]
    Automatic,
    [StringValue("EFF_LEN")]
    EffectiveLength,
    [StringValue("EXPLICIT")]
    Explicit
  }

  public enum Colour
  {
    NotSet = 0,
    NO_RGB,
    BLACK,
    MAROON,
    DARK_RED,
    RED,
    ORANGE_RED,
    DARK_GREEN,
    GREEN,
    OLIVE,
    DARK_ORANGE,
    ORANGE,
    GOLD,
    LAWN_GREEN,
    LIME,
    CHARTREUSE,
    YELLOW,
    DARK_GOLDENROD,
    SADDLE_BROWN,
    CHOCOLATE,
    GOLDENROD,
    FIREBRICK,
    FOREST_GREEN,
    OLIVE_DRAB,
    BROWN,
    SIENNA,
    DARK_OLIVE_GREEN,
    GREEN_YELLOW,
    LIMEGREEN,
    YELLOW_GREEN,
    CRIMSON,
    PERU,
    TOMATO,
    DARK_SLATE_GREY,
    CORAL,
    SEA_GREEN,
    INDIAN_RED,
    SANDY_BROWN,
    DIM_GREY,
    DARK_KHAKI,
    MIDNIGHT_BLUE,
    MEDIUM_SEA_GREEN,
    SALMON,
    DARK_SALMON,
    LIGHT_SALMON,
    SPRING_GREEN,
    NAVY,
    PURPLE,
    TEAL,
    GREY,
    LIGHT_CORAL,
    INDIGO,
    MEDIUM_VIOLET_RED,
    BURLYWOOD,
    DARK_BLUE,
    DARK_MAGENTA,
    DARK_SLATE_BLUE,
    DARK_CYAN,
    TAN,
    KHAKI,
    ROSY_BROWN,
    DARK_SEA_GREEN,
    SLATE_GREY,
    LIGHT_GREEN,
    DEEP_PINK,
    PALE_VIOLET_RED,
    PALE_GREEN,
    LIGHT_SLATE_GREY,
    MEDIUM_SPRING_GREEN,
    CADET_BLUE,
    DARK_GREY,
    LIGHT_SEA_GREEN,
    MEDIUM_AQUAMARINE,
    PALE_GOLDENROD,
    NAVAJO_WHITE,
    WHEAT,
    HOT_PINK,
    STEEL_BLUE,
    MOCCASIN,
    PEACH_PUFF,
    SILVER,
    LIGHT_PINK,
    BISQUE,
    PINK,
    DARK_ORCHID,
    MEDIUM_TURQUOISE,
    MEDIUM_BLUE,
    SLATE_BLUE,
    BLANCHED_ALMOND,
    LEMON_CHIFFON,
    TURQUOISE,
    DARK_TURQUOISE,
    LIGHT_GOLDENROD_YELLOW,
    DARK_VIOLET,
    MEDIUM_ORCHID,
    LIGHT_GREY,
    AQUAMARINE,
    PAPAYA_WHIP,
    ORCHID,
    ANTIQUE_WHITE,
    THISTLE,
    MEDIUM_PURPLE,
    GAINSBORO,
    BEIGE,
    CORNSILK,
    PLUM,
    LIGHT_STEEL_BLUE,
    LIGHT_YELLOW,
    ROYAL_BLUE,
    MISTY_ROSE,
    BLUE_VIOLET,
    LIGHT_BLUE,
    POWDERBLUE,
    LINEN,
    OLDLACE,
    SKYBLUE,
    CORNFLOWER_BLUE,
    MEDIUM_SLATE_BLUE,
    VIOLET,
    PALE_TURQUOISE,
    SEASHELL,
    FLORAL_WHITE,
    HONEYDEW,
    IVORY,
    LAVENDER_BLUSH,
    WHITE_SMOKE,
    LIGHT_SKY_BLUE,
    LAVENDER,
    SNOW,
    MINT_CREAM,
    BLUE,
    MAGENTA,
    DODGER_BLUE,
    DEEP_SKY_BLUE,
    ALICE_BLUE,
    GHOST_WHITE,
    CYAN,
    LIGHT_CYAN,
    AZURE,
    WHITE
  }
}
