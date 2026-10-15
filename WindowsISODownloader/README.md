# Windows ISO Downloader 3.6.0

3.5.0 özellikleri korunarak haftalık bağlantı kontrolü, Office özelleştirmesi, yeni ürünler, CLI, .resx çeviri altyapısı ve güncelleme doğrulaması eklendi. Ayrıntılar kökteki SURUM-3.6.0.md, CLI-KILAVUZ.md, DOGRULAMA-RAPORU.md ve YAYIN-KILAVUZU-3.6.0.md dosyalarında.

Kaynaklar: [ODT yapılandırması](https://learn.microsoft.com/en-us/microsoft-365-apps/deploy/office-deployment-tool-configuration-options), [Visual C++](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist), [Terminal](https://github.com/microsoft/terminal/releases), [ADK](https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install), [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads), [Visual Studio](https://visualstudio.microsoft.com/downloads/), [.NET yayın verisi](https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json).

# Windows ISO Downloader 3.6.0

Ürün, sürüm, mimari ve dil seçerek desteklenen Microsoft dosyalarını indiren Windows WPF uygulaması. Türkçe/English arayüz; Açık/Koyu/Sistem teması; ekran ve DPI uyumu.

## Çalıştırma
Bilgisayarınıza uygun Windows x64 veya ARM64 dağıtım ZIP dosyasının tamamını çıkarın. Windows ISO Downloader.exe, Fido.ps1 ve FIDO-LICENSE.txt aynı klasörde kalmalıdır. Fido değiştirilirse bütünlük kontrolü bağlantı çözümlemesini durdurur; özgün paketi yeniden çıkarın. Kendine yeterli EXE için kullanıcıya .NET SDK kurulumu gerekmez.

## Kaynaktan derleme
Windows üzerinde .NET8 SDK ile `dotnet publish WindowsISODownloader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` çalıştırılabilir. Kurulum.cmd kaynakları kısa yerel yola kopyalayarak derler. Repair.cmd SDK sağlık kontrolünü de çalıştırır; temel dosyaları eksik, boş veya MSBuild sorgusu başarısız olan yerel SDK yeniden kurulur. Sağlıklı SDK kod derleme hatası yüzünden silinmez. Derleme hatasında kurulum günlüğünü inceleyin; betik kod hatalarını SDK yeniden indirerek örtmez.

## Testler
Depo kökünden `dotnet run --project Tests/CatalogTests.csproj -c Release` ve `pwsh -NoProfile -File Tests/FidoRegression.ps1` çalıştırılır. Workflow aynı testleri yayından önce çalıştırır.

## Güvenlik ve sınırlar
İndirme yönlendirmeleri her adımdan önce kaynak türüne göre doğrulanır. Başlık sorgusu 45 saniye, veri bekleme 120 saniye, Fido çözümleme 3 dakika ile sınırlıdır; toplam büyük dosya indirme süresi sınırlanmaz. Yayıncı Edge/PowerToys SHA256 verisi sunduğunda içerik karşılaştırılır; ISO'ların hepsi için doğrulanmış beklenen hash sağlanmaz. ODT ve setup.exe çalıştırılmadan önce Windows Authenticode kontrolü geçerli Microsoft Corporation imzası ister. Ağ/paylaşım disklerinin boş alanı DriveInfo ile tahmin edilmez; gerçek yazma hataları kullanıcıya bildirilir.

Fido Pete Batard tarafından geliştirilen ayrı GPL-3.0-or-later bileşenidir. Değiştirilmiş tam betik ve FIDO-LICENSE.txt dağıtılır; uygulama Microsoft ürünü değildir. Uygulamanın kendi kaynak kodu için henüz ayrı kullanım lisansı seçilmemiştir; Fido lisansı otomatik olarak tüm uygulamaya atanmış değildir.

Gerçek Windows WPF açılışı, Authenticode çalışması ve bütün ürünlerde tam indirme testi bu Linux ortamında yapılamadı. SURUM-3.6.0.md ve DOGRULAMA-RAPORU.md ayrıntıları içerir.

## Office indirmesinden sonra kurulum
Office-Paket-<kimlik> klasöründe Office yükleme dosyaları, setup.exe, configuration.xml, Office-Kur.ps1 ve Office-Kur.cmd birlikte saklanır. Office-Kur.cmd kullanıcı tarafından çalıştırılır; kurulum otomatik başlamaz. Microsoft imzası yeniden doğrulanır ve Windows yönetici onayı istenir. Paket başka klasöre taşındığında kaynak yolu kurulum sırasında yeniden ayarlanır. Office2016 IMG bu ODT akışını kullanmaz.

## Windows çalışma kontrolleri
Kaynak kökünden Windows PowerShell ile `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tests/WindowsSmoke.ps1 -Exe "tam\yol\Windows ISO Downloader.exe"` çalıştırılabilir. İsteğe bağlı `-OdtPath "tam\yol\officedeploymenttool.exe"` geçerli Microsoft imzası ve salt okunur kilit altında çıkarma kontrolünü ekler; Office kurulmaz. CI x64 açılış, dosya kilidi ve PS5.1 kontrollerini çalıştıracak şekilde hazırlanmıştır. ARM64 paketi burada çapraz derlenmiştir; gerçek ARM64 açılışı ayrıca ARM64 Windows cihazında denenmelidir.

## 3.6.0 indirme ve sonrasında kullanım
Duraklatma/veri akışı hatasında .part ve .part.json dosyalarını silmeyin. Aynı ürün, sürüm, mimari ve dili seçip aynı son kayıt dosyasını (.iso/.exe vb., .part dosyasını değil) seçerek devam edin. Sunucu güçlü ETag ve Range desteği sunmuyorsa veya dosya değişmişse güvenli şekilde baştan indirilir. Geçici hatalarda üç yeniden deneme yapılır (ilk istekle birlikte en çok4). Süresi dolmuş ISO bağlantısı için yeniden indirme düğmesine basıldığında yeni bağlantı çözülür.

Tamamlanan dosyanın SHA256 değeri sonuç alanında gösterilir ve kopyalanabilir. Karşılaştır düğmesi dosyayı yeniden okuyup yapıştırılan değerle karşılaştırır. Hesaplanan değer tek başına Microsoft doğrulaması anlamına gelmez; beklenen değeri güvenilir kaynaktan alın. Edge/PowerToys yayıncı hash kontrolü ayrıca korunur.

Sonuç alanından klasörü açın, ISO'yu bağlayın veya EXE/MSI imza bilgilerini görüntüleyin. ZIP için içindeki EXE dosyaları geçici klasörde, çalıştırılmadan incelenir. İmza durumu ve imzalayan adı gösterilir; Windows sertifika zinciri/ağ koşulları sonucu etkileyebilir. Windows bildirimi sistem ayarlarına bağlıdır. Görev çubuğunda ilerleme, hız ve tahmini kalan süre gösterilir.

Ürün/mimari/dil/kayıt klasörü LOCALAPPDATA/WID/selections.json içine kaydedilir. İlk seçimde desteklenen yerlerde işletim sistemi mimarisi kullanılır.

Windows11/10 sürüm menüsünde güncel Microsoft seçeneğine ek olarak Fido -Rel List kataloğu bulunur. Katalog bir erişim garantisi değildir: seçilen sürümün dosya adına göre doğrulanamayan veya Microsoft'un artık sunmadığı bağlantılar reddedilir. Mevcut Fido1.70-WID.2 Windows11 kataloğunda 25H2 vardır, 24H2 yoktur; uygulama var olmayan arşiv URL'leri üretmez.
