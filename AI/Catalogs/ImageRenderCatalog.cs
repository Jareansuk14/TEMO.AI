namespace TEMO.AI.Ai;

internal static class ImageRenderCatalog
{
    public static readonly ImageStyle[] Realistic =
    [
        new("เสมือนจริง", "ภาพสมจริงเหมือนถ่ายจากกล้องจริง รายละเอียดชัด แสงธรรมชาติ องค์ประกอบอ่านง่าย"),
        new("ภาพถ่ายจริง", "ภาพถ่ายความละเอียดสูง โฟกัสคมชัด สีและแสงเหมือนภาพจากกล้องจริง"),
        new("Natural light", "ภาพถ่ายแสงธรรมชาติ เงานุ่ม สีสมจริง ดูเป็นธรรมชาติและน่าเชื่อถือ"),
        new("Studio portrait", "ภาพสตูดิโอ ตัวแบบเด่น แสงสวย ฉากหลังเรียบ องค์ประกอบสะอาด"),
        new("Softbox studio", "ภาพสตูดิโอด้วยแสงซอฟต์บ็อกซ์ ตัวแบบชัด แสงนุ่ม พื้นหลังไม่รก"),
        new("Product photography", "ภาพถ่ายสินค้าแบบสตูดิโอ วัตถุเด่น แสงควบคุม เงาธรรมชาติ"),
        new("Ecommerce photo", "ภาพถ่ายสินค้าอีคอมเมิร์ซ พื้นหลังสะอาด รายละเอียดชัด สีไม่เพี้ยน"),
        new("Commercial portrait", "ภาพถ่ายเชิงโฆษณา ตัวแบบจริง แสงสะอาด เหมาะกับภาพโปรโมชัน"),
        new("Beauty photography", "ภาพถ่ายบิวตี้ ใบหน้าและรายละเอียดชัด ผิวดูดีแต่ยังเป็นธรรมชาติ"),
        new("Fashion photography", "ภาพถ่ายแฟชั่นจากกล้องจริง เสื้อผ้าเด่น ท่าทางธรรมชาติ"),
        new("Lifestyle photography", "ภาพถ่ายไลฟ์สไตล์ คนหรือวัตถุในบรรยากาศจริง ดูเป็นมิตรและเข้าถึงง่าย"),
        new("Cinematic photo", "ภาพถ่ายโทนภาพยนตร์ แต่ยังสมจริง แสงและสีมีมิติแบบมืออาชีพ"),
        new("Minimal studio photo", "ภาพถ่ายสตูดิโอมินิมอล พื้นหลังเรียบ ตัวแบบเด่น เหมาะกับงานโปรโมชัน"),
        new("Clean background photo", "ภาพถ่ายพื้นหลังสะอาด ตัวแบบเด่นชัด เหมาะกับแบนเนอร์และโปรโมชั่น"),
        new("Natural color photo", "ภาพถ่ายสีธรรมชาติ ไม่แต่งสีหนัก ผิวและวัสดุดูเหมือนของจริง"),
        new("Warm tone photo", "ภาพถ่ายโทนอุ่น แสงนุ่ม ให้ความรู้สึกเป็นมิตรและน่าดึงดูด"),
        new("Cool tone photo", "ภาพถ่ายโทนเย็นแบบมืออาชีพ แสงสะอาด ภาพดูทันสมัย"),
        new("Modern smartphone photo", "ภาพถ่ายสไตล์สมาร์ตโฟนยุคใหม่ คมชัด สีธรรมชาติ ดูร่วมสมัย")
    ];

    public static readonly ImageStyle[] Stylized =
    [
        new("3D การ์ตูน", "เรนเดอร์ 3D การ์ตูน รูปทรงชัด สีสด ตัวแบบอ่านง่าย"),
        new("การ์ตูน 2D", "ภาพการ์ตูนสองมิติ เส้นสะอาด สีชัด องค์ประกอบเรียบง่าย"),
        new("อนิเมะ", "สไตล์อนิเมะญี่ปุ่น เส้นคม สีสว่าง ตัวละครเด่น ฉากไม่รก"),
        new("Chibi", "ตัวละครจิบิ หัวโต ตัวเล็ก สีสด น่ารัก มองเห็นชัด"),
        new("Kawaii", "สไตล์น่ารัก สีพาสเทล รูปทรงกลม เส้นนุ่ม เป็นมิตร"),
        new("Cute mascot", "มาสคอตน่ารัก รูปทรงจำง่าย สีสด เหมาะกับภาพโปรโมชัน"),
        new("Vector mascot", "มาสคอตเวกเตอร์ เส้นคม สีสด ตัวละครชัดและจำง่าย"),
        new("Flat vector", "เวกเตอร์แบน รูปทรงเรียบง่าย สีตัน อ่านภาพได้ทันที"),
        new("Gradient vector", "เวกเตอร์ไล่สีสมัยใหม่ รูปทรงสะอาด มีมิติเล็กน้อย"),
        new("Outline vector", "เวกเตอร์เส้นขอบชัด รายละเอียดน้อย เหมาะกับงานกราฟิกทั่วไป"),
        new("Minimal line", "ลายเส้นมินิมอล พื้นหลังสะอาด เส้นเรียบ รายละเอียดน้อย"),
        new("Sticker art", "ภาพสติกเกอร์ เส้นขอบชัด สีสด วัตถุหรือคาแรกเตอร์เด่น"),
        new("Infographic", "ภาพอินโฟกราฟิก ไอคอนชัด จัดข้อมูลเป็นระบบ อ่านง่าย"),
        new("Isometric", "ภาพมุมไอโซเมตริก จัดวางเป็นระเบียบ มององค์ประกอบง่าย"),
        new("Low poly", "รูปทรงโลว์โพลีเหลี่ยมชัด สีเรียบ องค์ประกอบไม่ซับซ้อน"),
        new("Pixel art", "ภาพพิกเซลอาร์ต ขอบคม รายละเอียดเรียบง่าย มีเอกลักษณ์"),
        new("Watercolor", "ภาพสีน้ำ สีฟุ้งนุ่ม ดูสวยและเป็นมิตร องค์ประกอบไม่ซับซ้อน"),
        new("Marker illustration", "ภาพวาดมาร์กเกอร์ สีสด ขอบคม เงาเรียบแบบภาพประกอบ"),
        new("Comic book", "คอมิกตะวันตก เส้นหนา สีตัดกัน ตัวแบบโดดเด่น"),
        new("Pop art", "ป๊อปอาร์ต สีสด เส้นชัด จุดสนใจเด่น เหมาะกับงานโปรโมชัน"),
        new("Retro poster", "โปสเตอร์ย้อนยุค สีจำกัด รูปทรงเด่น องค์ประกอบชัด"),
        new("Art deco", "อาร์ตเดโค เส้นเรขาคณิต สีหรู องค์ประกอบสะอาด"),
        new("Memphis", "สไตล์เมมฟิส สีสด ลวดลายเรขาคณิต สนุกและอ่านง่าย"),
        new("Vaporwave", "วาเปอร์เวฟ สีชมพูม่วง ฟ้าพาสเทล ดูโดดเด่นและทันสมัย"),
        new("Synthwave", "ซินธ์เวฟ นีออนย้อนยุค สีจัด จุดเด่นชัด"),
        new("Fantasy art", "แฟนตาซีสวยงาม ตัวแบบเด่น แสงสวย ฉากไม่รก"),
        new("Sci-fi art", "ภาพไซไฟ แสงเทคโนโลยี องค์ประกอบชัด ดูล้ำสมัย"),
        new("Minimal 3D", "เรนเดอร์ 3D เรียบง่าย รูปทรงชัด พื้นหลังสะอาด"),
        new("Clay 3D", "โมเดลดินปั้น 3D รูปทรงนุ่ม สีชัด พื้นหลังเรียบ"),
        new("Plastic toy", "เรนเดอร์เหมือนของเล่นพลาสติก ผิวเรียบ เงานุ่ม สีสด"),
        new("Paper cut", "กระดาษตัดซ้อนชั้นแบบเรียบ เงานุ่ม องค์ประกอบชัด"),
        new("Glassmorphism 3D", "3D ใสเหมือนกระจก เงาอ่อน ดูทันสมัยและสะอาด"),
        new("Claymorphism", "3D นุ่มแบบดินหรือยาง รูปทรงกลม เงานุ่ม สีพาสเทล"),
        new("Landing page illustration", "ภาพประกอบเว็บ สีสะอาด ตัวแบบและองค์ประกอบเรียบง่าย"),
        new("SaaS illustration", "ภาพประกอบแนวซอฟต์แวร์ มีไอคอนหรือหน้าจอแบบเรียบง่าย"),
        new("Tech startup illustration", "ภาพประกอบสตาร์ทอัพเทคโนโลยี สีสด ตัวแบบเรียบง่าย"),
        new("Finance illustration", "ภาพประกอบการเงิน กราฟ เหรียญ ไอคอน สีสะอาด"),
        new("Medical illustration", "ภาพประกอบการแพทย์แบบเรียบง่าย ไอคอนและตัวแบบสะอาด"),
        new("Education illustration", "ภาพประกอบการศึกษา หนังสือ กระดาน ตัวแบบอ่านง่าย"),
        new("Marketing illustration", "ภาพประกอบการตลาด สีสด สัญลักษณ์ชัด เหมาะกับแบนเนอร์"),
        new("Ecommerce illustration", "ภาพประกอบร้านค้าออนไลน์ กล่องสินค้า รถเข็น ไอคอนเรียบ"),
        new("Real estate illustration", "ภาพประกอบอสังหา บ้าน อาคาร ตัวแบบเวกเตอร์ สีสะอาด"),
        new("Beauty illustration", "ภาพประกอบความงาม ใบหน้า เครื่องสำอาง เส้นเรียบ สีพาสเทล"),
        new("Food illustration", "ภาพประกอบอาหาร สีสด รูปทรงน่าดึงดูด อ่านง่าย"),
        new("Botanical illustration", "ภาพประกอบพืชหรือดอกไม้ เส้นสวย สีสะอาด รายละเอียดพอดี"),
        new("Animal cartoon", "สัตว์การ์ตูน รูปทรงน่ารัก สีสด ใบหน้าแสดงอารมณ์ชัด"),
        new("Children book", "ภาพประกอบหนังสือเด็ก สีอบอุ่น รูปทรงอ่านง่าย ตัวแบบเป็นมิตร"),
        new("Pattern art", "ภาพลวดลายซ้ำต่อเนื่อง สีและรูปทรงจัดวางเป็นระบบ"),
        new("Seamless pattern", "แพทเทิร์นไร้รอยต่อ ใช้ซ้ำเป็นพื้นหลังหรือองค์ประกอบกราฟิก"),
        new("Graffiti", "กราฟฟิตี้ สีสด ตัวอักษรหรือรูปทรงเด่น ให้อารมณ์ทันสมัย"),
        new("Street art", "ภาพสตรีทอาร์ต สีจัด จุดเด่นชัด ดูมีพลัง"),
        new("Candy style", "ภาพสไตล์ลูกกวาด สีหวาน ผิวเงา รูปทรงกลม ดูสะดุดตา"),
        new("Marshmallow 3D", "3D นุ่มเหมือนมาร์ชเมลโลว์ สีพาสเทล เงานุ่ม ดูเป็นมิตร"),
        new("Crystal art", "ภาพผลึกคริสตัล รูปทรงเหลี่ยม แสงสวย ดูโดดเด่น"),
        new("Gemstone 3D", "3D อัญมณี เหลี่ยมคม แสงประกาย สีสด ดูพรีเมียม")
    ];

    public static readonly ImageStyle[] All = [.. Realistic, .. Stylized];

    public static ImageStyle Random(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);

        return PromptUsedStore.PickAtomic("render", used =>
        {
            var first = rng.Next(2) == 0 ? Realistic : Stylized;
            var available = first.Where(s => !used.Contains(s.Name)).ToList();

            if (available.Count == 0)
            {
                var other = first == Realistic ? Stylized : Realistic;
                available = other.Where(s => !used.Contains(s.Name)).ToList();
            }

            if (available.Count == 0)
            {
                used.Clear();
                available = first.ToList();
            }

            var pick = available[rng.Next(available.Count)];
            used.Add(pick.Name);
            return pick;
        });
    }

    public static ImageStyle RandomRealistic(Random rng) =>
        PromptUsedStore.Pick("render", Realistic, s => s.Name, rng, 1)[0];

    public static ImageStyle RandomStylized(Random rng) =>
        PromptUsedStore.Pick("render", Stylized, s => s.Name, rng, 1)[0];
}
