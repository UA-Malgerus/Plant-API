using System.Text.RegularExpressions;

namespace Plant_API.Constants;

public class PlantConstants
{
    public static readonly string[] Keywords = new[]
    {
        "plant", "рослин", "flora", "флор", "veget", "вегет", "botan", "ботан", "чагарник", "напівчагарник", "shrub",
        "дерев", "tree", "трав", "herb", "злак", "grass", "ліан", "vine",

        // Частини рослин
        "root", "корен", "stem", "стебл", "leaf", "лист", "petiol", "черешк",
        "bud", "бруньк", "shoot", "пагон", "branch", "гілк", "trunk", "стовбур",
        "bark", "кора", "коріння", "корою", "cambium", "камб", "xylem", "ксилем", "phloem", "флоем",

        // Квітка та її частини
        "flower", "квіт", "blossom", "цвіт", "infloresc", "суцвіт", "petal", "пелюст",
        "sepal", "чашолист", "stamen", "тичинк", "pistil", "маточк",
        "anther", "пиляк", "stigma", "приймальц",
        "nectar", "нектар", "pollin", "пилк", "pollen", "пилок", "пилк",

        // Плоди та насіння
        "fruit", "плод", "плід", "berry", "ягод", "seed", "насін", "nut", "горіх", "grain", "зерн",
        "caryops", "зернівк", "achene", "сім'янк", "drupe", "кістянк", "pod", "стручк", "legume", "боб",

        // Листя та його частини
        "leaflet", "листочк",
        "stipule", "прилистк",

        // Типи рослин
        "tree", "дерев", "shrub", "кущ", "herb", "трав", "grass", "злак", "vine", "ліан",
        "moss", "мох", "fern", "папорот", "algae", "водорост", "fungus", "гриб",

        // Розмноження та розвиток
        "germinat", "пророст", "sprout", "парост",
        "pollinat", "запил", "dispers", "розповсюдж", "seedling", "саджанц"
    };

    public static readonly Regex KeywordRegex = new(
        @"\b(" + string.Join("|", Keywords) + @")\w*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );
}