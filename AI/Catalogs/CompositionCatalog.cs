namespace TEMO.AI.Ai;

internal static class CompositionCatalog
{
    public static readonly string[] RealisticModel =
    [
        "ผู้ชายผิวขาว",
        "ผู้ชายผิวดำ",
        "ผู้ชายผิวแทน",
        "ผู้ชายเอเชีย",
        "ผู้ชายยุโรป",
        "ผู้หญิงผิวขาว",
        "ผู้หญิงผิวแทน",
        "ผู้หญิงเอเชีย",
        "ผู้ชายสไตล์นักธุรกิจ",
        "ผู้หญิงสไตล์แฟชั่น",
    ];

    public static readonly string[] StylizedModel =
    [
        "นักรบเกราะทอง",
        "เจ้าหญิงเวทมนตร์",
        "โจรสลัดทะเลแฟนตาซี",
        "นักผจญภัยหนุ่ม",
        "แม่มดป่าเวทมนตร์",
        "อัศวินดาบแสง",
        "นักล่าสัตว์ประหลาด",
        "เด็กสาวเวทมนตร์",
        "ราชามังกร",
        "นักเล่นแร่แปรธาตุ",
    ];

    public static readonly IReadOnlyDictionary<string, string[]> GameModel = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Human"] =
        [
            "ผู้ชายผิวขาว",
            "ผู้ชายผิวดำ",
            "ผู้ชายผิวแทน",
            "ผู้ชายผิวสีน้ำผึ้ง",
            "ผู้ชายเอเชีย",
            "ผู้ชายยุโรป",
            "ผู้หญิงผิวขาว",
            "ผู้หญิงเอเชีย",
            "ผู้หญิงสไตล์แฟชั่น",
            "ผู้ชายสไตล์นักธุรกิจ",
        ],
        ["Animal"] =
        [
            "เสือโคร่งเวทมนตร์",
            "มังกรควันไฟ",
            "สิงโตคิงของป่า",
            "อินทรีทอง",
            "หมาป่าจันทรา",
            "งูเงินสีรุ้ง",
            "วัวกระทิงพาันธง",
            "ปลาวาฬฟ้า",
            "แมวป่าเงา",
            "ม้าสีเพลิง",
        ],
    };

    private const int MainCastCount = 6;

    public static List<string> PickMainCast(Random rng, bool realistic)
    {
        var pool = (realistic ? RealisticModel : StylizedModel).ToList();
        var count = Math.Min(MainCastCount, pool.Count);
        var cast = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var index = rng.Next(pool.Count);
            cast.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return cast;
    }

    public static List<string> PickGameCast(Random rng, int count)
    {
        if (GameModel.Count == 0 || count <= 0) return [];
        var keys = GameModel.Keys.ToList();
        var pool = GameModel[keys[rng.Next(keys.Count)]].ToList();
        var cast = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            if (pool.Count == 0) pool = GameModel[keys[rng.Next(keys.Count)]].ToList();
            var index = rng.Next(pool.Count);
            cast.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return cast;
    }

    public static string Compose(Random rng, IReadOnlyList<string> mainCast, int minTotal, int maxTotal)
    {
        if (mainCast.Count == 0 || maxTotal <= 0) return "";

        var lo = Math.Max(1, minTotal);
        var hi = Math.Max(lo, Math.Min(maxTotal, mainCast.Count));
        var total = rng.Next(lo, hi + 1);

        var pool = mainCast.ToList();
        var chosen = new List<string>(total);
        for (var i = 0; i < total; i++)
        {
            var index = rng.Next(pool.Count);
            chosen.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return string.Join(" กับ ", chosen);
    }
}
