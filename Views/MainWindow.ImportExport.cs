namespace TEMO.AI;

public partial class MainWindow
{
    internal enum AiPromptType { Casino, Lottery, Slot }

    private AiPromptType SelectedAiPromptType =>
        PromptTypeLottery?.IsChecked == true ? AiPromptType.Lottery
        : PromptTypeSlot?.IsChecked == true ? AiPromptType.Slot
        : AiPromptType.Casino;

    private const string PromptFormatRules =
        "กฎเหล็กที่ต้องทำตามอย่างเคร่งครัด:\r\n" +
        "1. ตอบกลับมาเฉพาะบรรทัด id=...\"...\" เท่านั้น ห้ามมีคำอธิบาย หัวข้อ หรือข้อความอื่นใดทั้งสิ้น\r\n" +
        "2. รูปแบบทุกบรรทัดต้องเป็น:  id=ชื่อid\"เนื้อหา\"\r\n" +
        "3. ใช้เครื่องหมาย \" เปิดและปิดเนื้อหาเสมอ ห้ามใช้เครื่องหมายอื่น\r\n" +
        "4. 1 id = 1 บรรทัด ห้ามขึ้นบรรทัดใหม่ภายในเนื้อหาของ id เดียวกัน\r\n" +
        "5. ตอบกลับมาให้ครบทุก id ที่ส่งไป ห้ามข้ามหรือละเว้น\r\n\r\n";

    private const string PromptCommonTail =
        "- heading (id ที่ไม่ขึ้นต้นด้วย sub-): กระชับ จับใจ ยาวไม่เกิน 80 ตัวอักษร ห้ามใส่ชื่อแบรนด์ในหัวข้อเด็ดขาด\r\n" +
        "- body/description (id ที่ขึ้นต้นด้วย sub-): เนื้อหาแน่น ละเอียด ความยาว 200 ตัวอักษรขึ้นไป แต่ยาวไม่เกิน 400 ตัวอักษร มีข้อมูลจูงใจ keyword และอาจกล่าวถึงชื่อแบรนด์ได้\r\n" +
        "- ใช้ภาษาไทยที่เป็นธรรมชาติ อ่านแล้วน่าเชื่อถือ กระตุ้นให้คลิก ห้ามใช้คำเชื่อมติดนิสัย AI เช่น \"ในยุคปัจจุบัน\", \"อย่างไรก็ตาม\", \"ท้ายที่สุดนี้\"\r\n" +
        "- เนื้อหาที่ส่งมาอาจมีชื่อแบรนด์เก่าปะปนอยู่ ให้ถือว่าชื่อแบรนด์ที่ระบุไว้ด้านบนเท่านั้นคือชื่อที่ถูกต้อง ห้ามใช้ชื่อแบรนด์อื่นใดในเนื้อหาที่ตอบกลับ\r\n\r\n";

    private const string PromptFaqRules =
        "- FAQ คำถาม (id ที่ขึ้นต้นด้วย faq-q-): ตั้งจากคำถามที่มีปริมาณการค้นหาจริงแบบ People Also Ask ที่ตรงใจผู้เล่น กระชับ ยาวไม่เกิน 80 ตัวอักษร ห้ามใส่ชื่อแบรนด์\r\n" +
        "- FAQ คำตอบ (id ที่ขึ้นต้นด้วย faq-a-): ตอบตรงประเด็นทันทีตั้งแต่ประโยคแรก สั้นกระชับ 2-3 ประโยค ยาวไม่เกิน 400 ตัวอักษร เพื่อรองรับ Featured Snippet และ FAQ Schema ของ Google แทรก keyword อย่างเป็นธรรมชาติ และอาจกล่าวถึงชื่อแบรนด์ได้\r\n\r\n";

    private const string PromptCasino =
        "คุณคือ SEO Copywriter ผู้เชี่ยวชาญอุตสาหกรรม iGaming (คาสิโนออนไลน์) ประสบการณ์กว่า 10 ปี จงเขียนเนื้อหาใหม่ทั้งหมดให้ทุก id ที่ส่งมา\r\n\r\n" +
        PromptFormatRules +
        "ข้อกำหนดเนื้อหา:\r\n" +
        "- เนื้อหาต้องแตกต่างจากเดิมโดยสิ้นเชิง ห้ามลอกเลียนหรือเรียบเรียงใหม่\r\n" +
        "- ใส่ Keyword คาสิโนออนไลน์ที่ค้นหาสูง เช่น คาสิโนออนไลน์ บาคาร่า คาสิโนสด สูตรบาคาร่า เว็บพนันออนไลน์ เว็บตรง อย่างเป็นธรรมชาติ ห้าม Keyword Stuffing\r\n" +
        "- โทนเสียงน่าเชื่อถือ เป็นมืออาชีพ แฝงความสนุกตื่นเต้น แต่ไม่โฆษณาชวนเชื่อเกินจริง (ห้ามใช้คำว่า \"รวยทางลัด\" หรือ \"ได้เงิน 100%\")\r\n" +
        PromptCommonTail;

    private const string PromptLottery =
        "คุณคือผู้เชี่ยวชาญด้านกลยุทธ์ Content SEO และนักเขียนอาวุโสสายหวยออนไลน์ของไทย จงเขียนเนื้อหาใหม่ทั้งหมดให้ทุก id ที่ส่งมา\r\n\r\n" +
        PromptFormatRules +
        "ข้อกำหนดเนื้อหา:\r\n" +
        "- เนื้อหาต้องแตกต่างจากเดิมโดยสิ้นเชิง ห้ามลอกเลียนหรือเรียบเรียงใหม่\r\n" +
        "- ใส่ Keyword หวยออนไลน์ที่ค้นหาสูง เช่น หวยออนไลน์ แทงหวย เลขเด็ด หวยจ่ายตรง ระบบออโต้ ไม่มีเลขอั้น อย่างเป็นธรรมชาติ ห้าม Keyword Stuffing\r\n" +
        "- โทนเสียงเป็นมืออาชีพ น่าเชื่อถือ สนุกสนาน เร้าใจ ชวนให้ติดตามและกระตุ้นยอดสมัคร\r\n" +
        PromptCommonTail;

    private const string PromptSlot =
        "คุณคือ Senior iGaming Content Specialist และผู้เชี่ยวชาญ Semantic SEO สายสล็อต จงเขียนเนื้อหาใหม่ทั้งหมดให้ทุก id ที่ส่งมา\r\n\r\n" +
        PromptFormatRules +
        "ข้อกำหนดเนื้อหา:\r\n" +
        "- เนื้อหาต้องแตกต่างจากเดิมโดยสิ้นเชิง ห้ามลอกเลียนหรือเรียบเรียงใหม่\r\n" +
        "- ใส่ Keyword สล็อตที่ค้นหาสูง เช่น สล็อตเว็บตรง สล็อตแตกง่าย ค่ายเกม RTP ฟรีสปิน ฝากถอนออโต้ อย่างเป็นธรรมชาติ ห้าม Keyword Stuffing\r\n" +
        "- สอดแทรกความรู้สึกแบบ \"ประสบการณ์ตรง\" (First-hand Experience) เพื่อสร้างความน่าเชื่อถือตามหลัก E-E-A-T เน้นข้อมูลเชิงลึก ไม่ใช่ทฤษฎีกว้างๆ\r\n" +
        PromptCommonTail;

    private static string PromptFor(AiPromptType type) => type switch
    {
        AiPromptType.Lottery => PromptLottery,
        AiPromptType.Slot => PromptSlot,
        _ => PromptCasino,
    };

    private static readonly Dictionary<string, string[]> SectionMap = new()
    {
        ["BRAND"] = ["brand"],
        ["HERO"] = ["main-seo", "sub-main-seo"],
        ["SEO"] = ["seo-"],
        ["CTA"] = ["cta-seo", "sub-cta-seo"],
        ["PROMOTION-SECTION"] = ["promo-comp-"],
        ["PROMOTIONS-PAGE"]   = ["promotion-seo", "sub-promo-seo"],
        ["CONTACT"] = ["contact-seo", "sub-cont-seo"],
        ["FAQ"] = ["faq-"],
    };

    private static bool IsSectionSelected(string section, string fieldId) =>
        SectionMap.TryGetValue(section, out var prefixes)
        && prefixes.Any(p => p.EndsWith('-') ? fieldId.StartsWith(p) : fieldId == p);

    internal (string body, int count) BuildPromptBody(HashSet<string> selected) =>
        BuildPromptBody(selected, SelectedAiPromptType);

    internal (string body, int count) BuildPromptBody(HashSet<string> selected, AiPromptType type)
    {
        var brandName = _boxes.TryGetValue("brand", out var brandBox) ? brandBox.Text.Trim() : "";

        var sb = new StringBuilder(PromptFor(type));

        if (selected.Contains("FAQ"))
            sb.Append(PromptFaqRules);

        if (!string.IsNullOrEmpty(brandName))
            sb.AppendLine($"ชื่อแบรนด์ของเรา: {brandName} (ใช้ได้ใน body/description เท่านั้น ห้ามใส่ใน heading)\r\n");

        int count = 0;
        foreach (var f in _fields)
        {
            if (f.Id == "brand") continue;
            if (!_boxes.TryGetValue(f.Id, out var box)) continue;
            var value = box.Text.Trim();
            if (string.IsNullOrEmpty(value)) continue;
            if (!selected.Any(sec => IsSectionSelected(sec, f.Id))) continue;
            sb.AppendLine(LineCodec.FormatContentLine(f.Id, value));
            count++;
        }
        return (sb.ToString(), count);
    }
}
