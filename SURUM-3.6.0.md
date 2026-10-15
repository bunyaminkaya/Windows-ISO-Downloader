# Windows ISO Downloader 3.6.0
15 Ekim 2026 (Türkiye)

## Eklenenler
- Haftalık ServerCatalog sağlık workflow’u: Pazartesi 06:17 UTC / 09:17 Türkiye; HEAD, desteklenmediğinde Range GET; medya gövdesi indirilmez. Hatalar üç denemeden sonra raporlanır. 403/429 ve geçici sunucu hataları belirsiz olarak sınıflanır. Kesin arızalar için tek issue oluşturulur/güncellenir; tüm kaynaklar sağlıklıysa issue kapanır.
- Office: Current, MonthlyEnterprise, SemiAnnual; Office 2019’da PerpetualVL2019. Altı mevcut Office dili birden fazla seçilebilir. Uygulama hariç tutma, Visio/Project ürünleri ve indirme tamamlandıktan sonra isteğe bağlı kurulum. Office 2016 IMG seçeneği ODT özelleştirmesine dahil değildir.
- Visual C++ v14 Redistributable; Windows Terminal; Windows ADK ve Windows PE eklentisi; SQL Server 2025 Standard/Enterprise Developer ve 2022 Evaluation önyükleyicileri; Visual Studio 2026/2022 Community ve Build Tools; desteklenen kararlı .NET SDK, Runtime, Desktop Runtime, ASP.NET Core Runtime.
- Windows Server 2025 VHDX, 2022/2019 VHD. Bu kaynaklar İngilizce x64’tür. Core / Desktop Experience aynı ISO’nun kurulumunda seçilir.
- EXE içinde başsız CLI: ürün, sürüm, mimari, dil, klasör, JSON, bağlantı çıktısı, SHA-256 karşılaştırması ve Office seçenekleri. Kullanımı CLI-KILAVUZ.md içindedir.
- 221 Türkçe/İngilizce .resx metin anahtarı. Yeni dil ekleme altyapısı; yeni çeviri dilleri bu sürümde sunulmuyor.
- GitHub Release güncelleme bilgisi. Başlangıçta yalnızca metadata kontrol edilir. Yeni sürümü doğrula düğmesi geçici paket indirir; SHA-256, EXE sürümü ve mevcut EXE ile aynı yayıncının geçerli Authenticode imzası doğrulanmadan doğrulandı mesajı verilmez. Otomatik kurulum yoktur.

## Önemli sınırlar
Bu paketler burada Authenticode ile imzalanmadı. Bu nedenle yayıncı imzası isteyen güncelleme doğrulaması, imzasız mevcut/yeni EXE ile başarıya ulaşmaz. Microsoft Artifact Signing tamamlandıktan sonra kullanılabilir. Hash tek başına yayıncı imzasının yerine geçmez.

Programın ARM64 sürümü aynı özellikleri içerir; Microsoft’un yalnızca x64 sunduğu SQL/Server dosyaları ARM64 olarak yeniden etiketlenmez. Visual Studio ve ADK seçenekleri önyükleyicidir; hedef mimari/uyumluluk ilgili kurulum aracında belirlenir. Microsoft 365 ODT 64-bit seçimi Windows ARM64 cihazlarda Microsoft’un desteklediği kurulum akışını kullanır; Office 2019 için yerel ARM64 iddiası yoktur.

Workflow dosyaları GitHub deposunun varsayılan dalına yüklendiğinde haftalık kontrol başlayabilir. Bu ortamdan depoya gönderim veya Actions çalıştırması yapılmadı.
