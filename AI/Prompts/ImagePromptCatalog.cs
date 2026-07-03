namespace TEMO.AI.Ai;

internal static class ImagePromptCatalog
{
    public static string ThemeLabel(AiPromptType type) => type switch
    {
        AiPromptType.Lottery => "หวยออนไลน์",
        AiPromptType.Slot => "สล็อตออนไลน์",
        _ => "คาสิโนออนไลน์",
    };

    public static string ThemeLabelEn(AiPromptType type) => type switch
    {
        AiPromptType.Lottery => "lottery",
        AiPromptType.Slot => "slot casino",
        _ => "casino",
    };

    public static string BuildPrompt(
        ImagePlanItem item, GenerationOptions options, ThemePalette palette,
        string composition, string promo, bool hasLogoReference)
    {
        var theme = ThemeLabel(options.ContentType);
        var style = options.Style ?? ImageStyleCatalog.All[0];
        var render = options.Render ?? ImageRenderCatalog.All[0];

        if (item.Id == "logo")
        {
            var themeEn = ThemeLabelEn(options.ContentType);
            if (ImageRenderCatalog.IsRealistic(render))
                return
                    $"You are an expert in website logo design. Please design a luxury {themeEn} wordmark logo for \u201c{options.Brand}\u201d. " +
                    $"Do not interpret the company name literally when designing the logo, with {themeEn} elements integrated directly into the letters. " +
                    $"Style: formal, elegant, premium, professional, clearly{themeEn}-related. " +
                    "Not too minimal, not cluttered, no slogan, no separate icon, no multi-part layout. " +
                    "May or may not be a background image behind the company name. " +
                    "Use medium-to-bold lettering for strong readability on dark and light backgrounds. " +
            $"Colors: {palette.Primary}, {palette.Secondary}, {palette.Accent}, plus gold / silver if needed. " +
            $"{FlatBgEn} 512x512px.";

            return
                "You are an expert in website logo design.\n" +
                $"Please design an art {themeEn} wordmark logo for \u201c{options.Brand}\u201d.\n" +
                $"Do not interpret the company name literally when designing the logo, with {themeEn} elements integrated directly into the letters.\n" +
                $"Style: formal, premium, professional, clearly {themeEn}-related.\n" +
                $"Render: {render.Name}\n" +
                $"Theme: {style.Name}\n" +
                "Not too minimal, not cluttered, no slogan, no separate icon, no multi-part layout.\n" +
                "May or may not be a background image behind the company name.\n" +
                "Use medium-to-bold lettering for strong readability on dark and light backgrounds.\n" +
            $"Colors: {palette.Primary}, {palette.Secondary}, {palette.Accent}.\n" +
            $"{FlatBgEn} 512x512px.";
        }

        var subject = string.IsNullOrEmpty(composition)
            ? ""
            : $" โดยมี{composition} ให้แต่งกายและออกแบบตัวละครให้เข้ากับธีม{style.Name}หากเป็นมนุษย์ให้มีทรงผมหลากหลายไม่ซ้ำกัน";
        var backdrop = $" และมีองค์ประกอบอยู่เบื้องหลังที่สื่อถึง{theme}";
        var logo = hasLogoReference ? " และนำรูปโลโก้ที่แนบมาไปไว้ในภาพด้วย" : "";
        var logoSubtle = hasLogoReference ? " และนำรูปโลโก้ที่แนบมาไปไว้ในภาพ แต่ห้ามอยู่กลางภาพ กลมกลืนกับองค์ประกอบ" : "";
        var imageType = ImageGroupCatalog.ImageTypeOf(item.Id);

        var request = item.Id switch
        {
            "play" =>
                $"ขอปุ่มกด ธีม{style.Name} (ตีความเป็นกราฟิกปุ่มเท่านั้น ไม่ใช่ภาพถ่ายของของจริง) พื้นหลังโปร่งใส มีข้อความว่า \"{RandomPlayText()}\" อยู่กลางปุ่ม อ่านชัด ไม่มีคำอื่น ไม่มีไอคอน ห้ามวาดเหรียญ ทองคำ ธนบัตร หรือสิ่งของจริงใด ๆ",
            "banner" =>
                $"ขอรูปภาพแบนเนอร์หลัก ธีม{style.Name}{subject}{backdrop}{logo} จัดองค์ประกอบเด่นเห็นชัด พร้อมใส่สโลแกนภาษาไทยสั้นๆที่เกี่ยวกับ{theme}{BgSubtle}",
            "background" =>
                $"ขอรูปภาพพื้นหลังเว็บ ธีม{style.Name}{backdrop} ไม่มีข้อความ ไม่มีตัวเลข ไม่มีโลโก้ ไม่มีตัวละครเด่น ใช้เป็นฉากหลังหลังเนื้อหาได้ดี",
            var s when s.StartsWith("btn-") =>
                $"ขอปุ่มกด ธีม{style.Name} (ตีความเป็นกราฟิกปุ่มเท่านั้น ไม่ใช่ภาพถ่ายของของจริง) พื้นหลังโปร่งใส มีข้อความภาษาไทยว่า \"{ButtonText(s["btn-".Length..])}\" อยู่กลางปุ่ม อ่านชัด ไม่มีคำอื่น ไม่มีไอคอน ห้ามวาดเหรียญ ทองคำ ธนบัตร หรือสิ่งของจริงใด ๆ",
            var s when s.StartsWith("promo-") =>
                $"ขอรูปภาพการ์ดโปรโมชั่น ธีม{style.Name}{subject}{logoSubtle} มีข้อความโปรโมชั่นภาษาไทยว่า \"{promo}\" เด่นชัดอ่านง่ายในภาพ ออกแบบให้ดูเป็นแบนเนอร์โปรโมชั่นน่าสนใจ{backdrop}{BgSubtle}",
            var s when s.StartsWith("seo-") =>
                $"ขอรูปภาพประกอบบทความ ธีม{style.Name}{subject}{logoSubtle} มีสโลแกนสั้นภาษาไทยว่า \"{promo}\" เด่นชัดอ่านง่ายในภาพ สื่อความหมายชัดเจน ไม่มีบล็อกข้อความอื่นที่อ่านได้{backdrop}{BgSubtle}",
            var s when s.StartsWith("game-") =>
                $"ขอรูปภาพการ์ดเกมแนวตั้ง ธีม{style.Name} ไม่มีข้อความ ตัวเลข หรือโลโก้ที่อ่านได้",
            _ when imageType == "transparent" && ImageGroupCatalog.ByPrefix(item.Id) is { Role.Length: > 0 } g =>
                $"ขอรูป{g.Role}แบบตัวละครเดี่ยวธีม{style.Name}{subject} เป็นวัตถุหลักเพียงตัวเดียว ไม่ใช่ปุ่ม ไม่ใช่โลโก้ ไม่มีข้อความ ไม่มีตัวเลข ไม่มีฉากหลัง",
            _ when ImageGroupCatalog.ByPrefix(item.Id) is { Role.Length: > 0 } g =>
                $"ขอรูปภาพ{g.Role} ธีม{style.Name}{subject}{backdrop} สื่อความหมายชัดเจน ไม่มีบล็อกข้อความอื่นที่อ่านได้",
            _ =>
                $"ขอรูปภาพประกอบเว็บ ธีม{style.Name}{backdrop} ไม่มีข้อความหรือโลโก้ที่อ่านได้",
        };

        var transparent = imageType is "button" or "transparent";
        var transparentNote = transparent
            ? "สำคัญ: พื้นหลังสีพื้นเรียบเพียงสีเดียว ตัดกับตัวละครอย่างชัดเจน โดยใช้สีที่ไม่ซ้ำและไม่มีอยู่ในตัวละครหรือเอฟเฟกต์ใดๆ ไม่มีลวดลาย ไม่มีเงา ไม่มีไล่สี เพื่อให้แยกตัวละครและลบพื้นหลังได้ง่าย\n"
            : "";

        return
            $"คุณคือผู้เชี่ยวชาญด้านกราฟฟิกดีไซน์ของรูปภาพที่ใช้บนเว็บไซต์ด้าน{theme}\n" +
            $"{request}\n" +
            $"สไตล์ภาพ: {render.Name}\n" +
            transparentNote +
            $"โทนสีเว็บ: สีหลัก {palette.Primary} สีรอง {palette.Secondary} สีเน้น {palette.Accent}\n" +
            "เพิ่มสีอื่นได้ตามความเหมาะสม แต่ต้องกลมกลืนเข้ากับโทนสีหลักเหล่านี้\n" +
            $"ขนาดภาพ: {item.Width}x{item.Height}";
    }

    public static string BuildGamePrompt(
        ImagePlanItem item, GenerationOptions options, ThemePalette palette,
        string character, bool transparent, bool hasProviderRef)
    {
        var theme = ThemeLabel(options.ContentType);
        var style = options.Style ?? ImageStyleCatalog.All[0];
        var render = options.Render ?? ImageRenderCatalog.All[0];
        var subject = string.IsNullOrWhiteSpace(character) ? "ตัวละครหรือมาสคอตเดี่ยว" : character;
        var provider = hasProviderRef
            ? $"และนำรูปโลโก้ค่ายเกมที่แนบมาวางไว้ในรูปด้วย แสดงโลโก้เป็นองค์ประกอบที่แยกออกจากตัวละคร แต่ให้วางทับซ้อนกับตัวละครในเชิงภาพ โดยห้ามนำโลโก้ไปรวมเป็นส่วนหนึ่งของชุด เกราะ สัญลักษณ์กลางอก ผ้าคลุม เข็มขัด หรืออุปกรณ์เสริม วางโลโก้ให้ทับซ้อนอยู่บริเวณส่วนล่างของตัวละคร ห้ามวางโลโก้ไว้ตรงกลางหน้าอก ให้วางเอาไว้บริเวณล่างของรูป และสร้างกรอบให้กับโลโก้ที่แนบไปให้เขากับธีม{style.Name} เพื่อทำให้โลโก้กับตัวละครดูกลมกลืนกัน ให้ขนาดของโลโก้คิดเป็น 25%ของภาพ"
            : "";
        var transparentNote = transparent
            ? "สำคัญ: พื้นหลังสีพื้นเรียบเพียงสีเดียว ตัดกับตัวละครหรือโลโก้อย่างชัดเจน โดยใช้สีที่ไม่ซ้ำและไม่มีอยู่ในตัวละครหรือเอฟเฟกต์ใดๆ ไม่มีลวดลาย ไม่มีเงา ไม่มีไล่สี เพื่อให้แยกตัวละครและลบพื้นหลังได้ง่าย\n"
            : "";
        var request = transparent
            ? $"ขอรูปการ์ดเกมแนวตั้งเป็น{subject}เพียงตัวเดียว ให้แต่งกายและออกแบบตัวละครให้เข้ากับธีม{style.Name} ไม่ใช่ปุ่ม ไม่ใช่โลโก้ ไม่มีข้อความ ไม่มีตัวเลข{provider}"
            : $"ขอรูปการ์ดเกมแนวตั้งเป็น{subject}เพียงตัวเดียว ให้แต่งกายและออกแบบตัวละครให้เข้ากับธีม{style.Name} มีฉากหลังที่สื่อถึง{style.Name} ไม่มีข้อความ ไม่มีตัวเลขที่อ่านได้{provider}";

        return
            $"คุณคือผู้เชี่ยวชาญด้านกราฟฟิกดีไซน์ของรูปภาพที่ใช้บนเว็บไซต์ด้าน{theme}\n" +
            $"{request}\n" +
            $"สไตล์ภาพ: {render.Name}\n" +
            transparentNote +
            $"โทนสีเว็บ: สีหลัก {palette.Primary} สีรอง {palette.Secondary} สีเน้น {palette.Accent}\n" +
            "เพิ่มสีอื่นได้ตามความเหมาะสม แต่ต้องกลมกลืนเข้ากับโทนสีหลักเหล่านี้\n" +
            $"ขนาดภาพ: {item.Width}x{item.Height}";
    }

    public static string BuildButtonReferencePrompt(ImagePlanItem item)
    {
        var isPlay = item.Id == "play";
        var text = isPlay ? RandomPlayText() : ButtonText(item.Id["btn-".Length..]);
        var langNote = isPlay
            ? "ข้อความอยู่กลางปุ่ม อ่านชัด ไม่มีคำอื่น ไม่มีไอคอน"
            : "ข้อความภาษาไทยอยู่กลางปุ่ม อ่านชัด ไม่มีคำอื่น ไม่มีไอคอน";
        return
            "คุณคือผู้เชี่ยวชาญด้านกราฟฟิกดีไซน์ของปุ่มกดบนเว็บไซต์\n" +
            $"โดยสร้างรูปให้เหมือนกับรูปที่แนบไป แต่เปลี่ยนคำข้างในเป็น \"{text}\"\n" +
            "คงสไตล์ รูปทรง สี ขนาด และองค์ประกอบทั้งหมดให้เหมือนรูปที่แนบไปทุกประการ เปลี่ยนเฉพาะข้อความเท่านั้น\n" +
            "ห้ามวาดเหรียญ ทองคำ ธนบัตร หรือสิ่งของจริงใด ๆ\n" +
            $"{langNote}\n" +
            "สำคัญ: พื้นหลังสีพื้นเรียบเพียงสีเดียว ตัดกับตัวละครอย่างชัดเจน โดยใช้สีที่ไม่ซ้ำและไม่มีอยู่ในตัวละครหรือเอฟเฟกต์ใดๆ ไม่มีลวดลาย ไม่มีเงา ไม่มีไล่สี เพื่อให้แยกตัวละครและลบพื้นหลังได้ง่าย\n" +
            $"ขนาดภาพ: {item.Width}x{item.Height}";
    }

    private static readonly string[] s_playTexts = ["Play", "เล่นเกม", "เล่นเลย"];

    private const string FlatBgEn =
        "Important: use a flat solid single-color background that contrasts sharply with the logo, " +
        "using a unique color not present in the logo or any effects — no patterns, no shadows, no gradients — " +
        "so the logo can be easily isolated and the background removed.";

    private const string BgSubtle =
        " พื้นหลังและฉากหลังมีรายละเอียดน้อย ไม่เยอะ เพื่อไม่ให้แย่งความเด่นไปจากข้อความและตัวละคร";

    private static string RandomPlayText() =>
        s_playTexts[Random.Shared.Next(s_playTexts.Length)];

    public static string ButtonText(string key) => key switch
    {
        "login" => "เข้าสู่ระบบ",
        "register" => "สมัครสมาชิก",
        "contact" => "ติดต่อเรา",
        _ => key,
    };

    public static string Alt(ImagePlanItem item, GenerationOptions options, string? caption = null)
    {
        if (!item.HasAlt) return item.Alt;

        if (!string.IsNullOrWhiteSpace(caption)
            && (item.Id.StartsWith("promo-", StringComparison.Ordinal)
                || item.Id.StartsWith("seo-", StringComparison.Ordinal)))
            return $"{options.Brand} {caption}";

        return item.Id switch
        {
            "banner" => $"{options.Brand} banner",
            "logo" => $"{options.Brand} logo",
            var s when s.StartsWith("btn-") => $"{options.Brand} ปุ่ม{ButtonText(s["btn-".Length..])}",
            var s when s.StartsWith("game-") => $"{options.Brand} เกม {int.Parse(s["game-".Length..]) + 1}",
            var s when s.StartsWith("promo-") => $"{options.Brand} โปรโมชั่น {int.Parse(s["promo-".Length..]) + 1}",
            var s when s.StartsWith("seo-") => $"{options.Brand} รูปบทความ SEO {s["seo-".Length..]}",
            _ => $"{options.Brand} {item.Label}",
        };
    }
}
