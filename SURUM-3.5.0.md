# Windows ISO Downloader 3.5.0 — x64 / ARM64

Eklenenler: güçlü ETag/If-Range/Content-Range doğrulamalı devam etme, duraklatmada kısmi veriyi saklama ve en çok3 otomatik tekrar; bütün tamamlanan dosyalarda SHA256 hesaplama/gösterme/kopyalama ve beklenen değerle yeniden okuyarak karşılaştırma; klasör açma, ISO bağlama, Windows bildirim balonu, TaskbarItemInfo, hız/kalan süre; son ürün/mimari/dil/kayıt klasörünü saklama ve desteklenen yerlerde OSArchitecture varsayılanı; EXE/MSI ve ZIP içindeki EXE'lerin imza durumunu/imzalayanını görüntüleme; Fido -Rel List ile eski sürüm seçimi ve dönen dosya adında seçilen sürümü doğrulama.

Sunucu Range/güçlü ETag sağlamazsa devam yerine baştan indirilir. SHA256 yalnızca hesaplanmışsa resmî kaynakça doğrulanmış sayılmaz. Eski sürümlerin erişimi Microsoft'a bağlıdır; mevcut Fido kataloğu 24H2 içermez. VHD paketinin staging klasörü hata/duraklatmada korunur; tamamlanan eski parçalar yeniden indirilerek kontrol edilir. Office paketinin configuration.xml/setup.exe/Office-Kur.cmd korunması sürer.

Windows native özellikleri her iki mimariye aynı kaynak kodla eklenmiştir; burada gerçek Windows'ta çalıştırılmadı. Mount-DiskImage/Authenticode ve Windows bildirimleri ilgili OS/izin ayarlarına tabidir. Kopyalama/bağlama/imza işlemleri kullanıcı düğmesine basınca gerçekleşir; indirme sonunda program otomatik kurulum yapmaz.
