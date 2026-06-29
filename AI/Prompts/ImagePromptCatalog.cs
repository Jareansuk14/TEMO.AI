namespace TEMO.AI.Ai;

internal static class ImagePromptCatalog
{
    public static string ThemeLabel(AiPromptType type) => type switch
    {
        AiPromptType.Lottery => "หวยออนไลน์",
        AiPromptType.Slot => "สล็อตออนไลน์",
        _ => "คาสิโนออนไลน์",
    };

    public static string BuildPrompt(
        ImagePlanItem item, GenerationOptions options, ThemePalette palette,
        string composition, string promo, bool hasLogoReference)
    {
        var theme = ThemeLabel(options.ContentType);
        var style = options.Style ?? ImageStyleCatalog.All[0];
        var render = options.Render ?? ImageRenderCatalog.All[0];
        var subject = string.IsNullOrEmpty(composition)
            ? ""
            : $" โดยมี{composition} ให้แต่งกายและออกแบบตัวละครให้เข้ากับธีม{style.Name}";
        var backdrop = $" และมีองค์ประกอบอยู่เบื้องหลังที่สื่อถึง{theme}";
        var logo = hasLogoReference ? " และนำรูปโลโก้ที่แนบมาไปไว้ในภาพด้วย" : "";
        var logoSubtle = hasLogoReference ? " และนำรูปโลโก้ที่แนบมาไปไว้ในภาพ แต่ห้ามอยู่กลางภาพ กลมกลืนกับองค์ประกอบ" : "";
        var imageType = ImageGroupCatalog.ImageTypeOf(item.Id);

        var request = item.Id switch
        {
            "logo" =>
                $"ขอโลโก้แบรนด์ \"{options.Brand}\" ที่สื่อถึง{theme} (ตีความเป็นกราฟิกโลโก้เท่านั้น ไม่ใช่ภาพถ่ายของของจริง) พื้นหลังโปร่งใส ข้อความแบรนด์อ่านชัด ไม่มีมาสคอต ไม่มีสโลแกน ห้ามวาดเหรียญ ทองคำแท่ง ธนบัตร อัญมณี หรือสิ่งของจริงใด ๆ ให้เป็นตราสัญลักษณ์นามธรรมเพียวๆ",
            "banner" =>
                $"ขอรูปภาพแบนเนอร์หลัก ธีม{style.Name}{subject}{backdrop}{logo} จัดองค์ประกอบเด่นเห็นชัด พร้อมใส่สโลแกนสั้นๆที่เกี่ยวกับ{theme}",
            "background" =>
                $"ขอรูปภาพพื้นหลังเว็บ ธีม{style.Name}{backdrop} ไม่มีข้อความ ไม่มีตัวเลข ไม่มีโลโก้ ไม่มีตัวละครเด่น ใช้เป็นฉากหลังหลังเนื้อหาได้ดี",
            var s when s.StartsWith("btn-") =>
                $"ขอปุ่มกดทรงโค้งมนพรีเมียม ธีม{style.Name} (ตีความเป็นกราฟิกปุ่มเท่านั้น ไม่ใช่ภาพถ่ายของของจริง) พื้นหลังโปร่งใส มีข้อความภาษาไทยว่า \"{ButtonText(s["btn-".Length..])}\" อยู่กลางปุ่ม อ่านชัด ไม่มีคำอื่น ไม่มีไอคอน ห้ามวาดเหรียญ ทองคำ ธนบัตร หรือสิ่งของจริงใด ๆ",
            var s when s.StartsWith("promo-") =>
                $"ขอรูปภาพการ์ดโปรโมชั่น ธีม{style.Name}{subject}{logoSubtle} มีข้อความโปรโมชั่นภาษาไทยว่า \"{promo}\" เด่นชัดอ่านง่ายในภาพ ออกแบบให้ดูเป็นแบนเนอร์โปรโมชั่นน่าสนใจ{backdrop}",
            var s when s.StartsWith("seo-") =>
                $"ขอรูปภาพประกอบบทความ ธีม{style.Name}{subject}{logoSubtle} มีสโลแกนสั้นภาษาไทยว่า \"{promo}\" เด่นชัดอ่านง่ายในภาพ สื่อความหมายชัดเจน ไม่มีบล็อกข้อความอื่นที่อ่านได้{backdrop}",
            var s when s.StartsWith("game-") =>
                $"ขอรูปภาพการ์ดเกมแนวตั้ง ธีม{style.Name} ไม่มีข้อความ ตัวเลข หรือโลโก้ที่อ่านได้",
            _ when imageType == "transparent" && ImageGroupCatalog.ByPrefix(item.Id) is { Role.Length: > 0 } g =>
                $"ขอรูป{g.Role}แบบตัวละครหรือมาสคอตเดี่ยวธีม{style.Name}{subject} เป็นวัตถุหลักเพียงตัวเดียว ไม่ใช่ปุ่ม ไม่ใช่โลโก้ ไม่มีข้อความ ไม่มีตัวเลข ไม่มีฉากหลัง",
            _ when ImageGroupCatalog.ByPrefix(item.Id) is { Role.Length: > 0 } g =>
                $"ขอรูปภาพ{g.Role} ธีม{style.Name}{subject}{backdrop} สื่อความหมายชัดเจน ไม่มีบล็อกข้อความอื่นที่อ่านได้",
            _ =>
                $"ขอรูปภาพประกอบเว็บ ธีม{style.Name}{backdrop} ไม่มีข้อความหรือโลโก้ที่อ่านได้",
        };

        var transparent = imageType is "button" or "transparent";
        var transparentNote = transparent
            ? "พื้นหลังต้องโปร่งใสจริง (alpha channel) ทั้งภาพ ไม่มีพื้นหลังสีทึบ ไม่มีพื้นหลังขาว ไม่มีกรอบหรือฉากหลัง วางวัตถุลอยบนพื้นโปร่งใสเท่านั้น\n"
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
            ? $"และนำรูปโลโก้ค่ายเกมที่แนบมาวางไว้ในรูปด้วย แสดงโลโก้เป็นองค์ประกอบที่แยกออกจากตัวละคร แต่ให้วางทับซ้อนกับตัวละครในเชิงภาพ โดยห้ามนำโลโก้ไปรวมเป็นส่วนหนึ่งของชุด เกราะ สัญลักษณ์กลางอก ผ้าคลุม เข็มขัด หรืออุปกรณ์เสริม วางโลโก้ให้ทับซ้อนอยู่บริเวณส่วนล่างของตัวละคร ห้ามวางโลโก้ไว้ตรงกลางหน้าอก ให้วางเอาไว้บริเวณล่างของรูป และใช้เอฟเฟคหรือโทนสีของธีม{style.Name} เพื่อทำให้โลโก้กับตัวละครดูกลมกลืนกัน ให้ขนาดของโลโก้คิดเป็น 25%ของภาพ"
            : "";
        var transparentNote = transparent
            ? "พื้นหลังต้องโปร่งใสจริง (alpha channel) ทั้งภาพ ไม่มีพื้นหลังสีทึบ ไม่มีพื้นหลังขาว ไม่มีกรอบหรือฉากหลัง วางตัวละครลอยบนพื้นโปร่งใส\n"
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
        var text = ButtonText(item.Id["btn-".Length..]);
        return
            "คุณคือผู้เชี่ยวชาญด้านกราฟฟิกดีไซน์ของปุ่มกดบนเว็บไซต์\n" +
            $"โดยสร้างรูปให้เหมือนกับรูปที่แนบไป แต่เปลี่ยนคำข้างในเป็น \"{text}\"\n" +
            "คงสไตล์ รูปทรง สี ขนาด และองค์ประกอบทั้งหมดให้เหมือนรูปที่แนบไปทุกประการ เปลี่ยนเฉพาะข้อความเท่านั้น\n" +
            "ห้ามวาดเหรียญ ทองคำ ธนบัตร หรือสิ่งของจริงใด ๆ\n" +
            "ข้อความภาษาไทยอยู่กลางปุ่ม อ่านชัด ไม่มีคำอื่น ไม่มีไอคอน\n" +
            "พื้นหลังต้องโปร่งใสจริง (alpha channel) ทั้งภาพ ไม่มีพื้นหลังสีทึบ ไม่มีพื้นหลังขาว ไม่มีกรอบหรือฉากหลัง\n" +
            $"ขนาดภาพ: {item.Width}x{item.Height}";
    }

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
