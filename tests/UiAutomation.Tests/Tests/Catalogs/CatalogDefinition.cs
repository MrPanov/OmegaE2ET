namespace UiAutomation.Tests.Tests.Catalogs;

public sealed record CatalogDefinition(string Name, int MenuIndex, string Route);

public static class CatalogDefinitions
{
    public static CatalogDefinition Tyres { get; } =
        new("Шини", 0, "#/app/simplesearchTires");

    public static CatalogDefinition WheelDiscs { get; } =
        new("Колісні диски", 13, "#/app/simplesearchWheelDisc");

    public static CatalogDefinition Cameras { get; } =
        new("Камери", 23, "#/app/simplesearchCameras");

    public static CatalogDefinition Oils { get; } =
        new("Оливи", 3, "#/app/simplesearchOil");

    public static CatalogDefinition TechnicalFluids { get; } =
        new("Тех. рідини", 11, "#/app/simplesearchTechnicalFluids");

    public static CatalogDefinition AgroParts { get; } =
        new("ЗЧ до сільгосптехніки", 21, "#/app/simplesearchAgro");

    public static CatalogDefinition Batteries { get; } =
        new("АКБ", 2, "#/app/simplesearchAccum");

    public static CatalogDefinition BodyAndOptics { get; } =
        new("Кузов та оптика", 1, "#/app/simplesearchOptic");

    public static CatalogDefinition Lamps { get; } =
        new("Лампи", 4, "#/app/simplesearchLamps");

    public static CatalogDefinition Bearings { get; } =
        new("Підшипники", 15, "#/app/simplesearchPodshipnik");

    public static CatalogDefinition AgroBelts { get; } =
        new("Ремені Агро техніка", 20, "#/app/simplesearchBelts");

    public static CatalogDefinition EmergencyConnectors { get; } =
        new("Аварійні з'єднувачі", 25, "#/app/simplesearchPneumo");
}
