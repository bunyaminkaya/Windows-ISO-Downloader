# Windows ISO Downloader 3.1

ZIP paketinin tamamını bir klasöre çıkarın, Windows ISO Downloader.exe dosyasını açın.
Fido.ps1 ve FIDO-LICENSE.txt EXE ile aynı klasörde kalmalıdır.
Windows x64 için kendine yeterli paket; .NET SDK kurmanız gerekmez.

## Yeni seçenekler
Windows Server sürüm menüsü: 2025 (mevcut), 2022, 2019, 2016, 2012 R2, 2012 Evaluation ISO.
Eklenen eski ISO seçenekleri İngilizce x64'tür. Türkçe ISO sunulmuyor.
Windows Embedded POSReady 2009: ayrı ürün, İngilizce x86, 120 günlük değerlendirme ISO'su.
Server 2022/2019/2016/2012 R2: 180 günlük değerlendirme.

Server 2008 R2 Enterprise x64, Server 2008 Standard x64, Server 2003 R2 Enterprise:
VHD değerlendirme paketi; ISO değildir. Server 2003 R2 için mimari resmî sayfada açıkça
belirtilmediğinden program Paket varsayılanı gösterir.
Kayıt klasörünü seçin. Tüm EXE/RAR parçaları WindowsServer_*_VHD alt klasörüne indirilir.
Tamamlanmadan paket klasörü yayımlanmaz. Hata/iptalde geçici klasör temizlenmeye çalışılır.
Aynı adlı hedef klasörü varsa işlem durur; başka kayıt klasörü seçin.
Arşiv otomatik çalıştırılmaz. Bütün parçaları aynı klasörde tutup part01.exe arşivini
elle çıkarın; çıkan VHD'yi uyumlu sanallaştırma yazılımında kullanın.

Türkçe/English arayüz ve Sistem/Koyu/Açık tema seçenekleri korunmuştur.
Bu uygulama Microsoft ürünü değildir. İndirme kullanım veya etkinleştirme lisansı vermez.

## Kontroller — 4 Ekim 2026
Release win-x64, self-contained tek EXE derlemesi başarılı.
Katalog testleri başarılı: ISO kaynağı, VHD parça sayısı/sırası, HTTPS kaynakları,
desteklenmeyen sürüm ve dilin reddedilmesi.
Üç VHD paketindeki 10 dosyanın tamamı HTTP HEAD 200 ve dosya boyutu döndürdü.
ISO bağlantılarının erişimi aynı gün kontrol edildi.
XAML dosyaları XML olarak doğrulandı.
Windows üzerinde açılış/arayüz, iptal ve GB boyutunda tam indirme/kurulum testleri yapılmadı.
Dosyaların SHA-256 orijinallik doğrulaması yapılmadı. EXE imzasızdır;
Artifact Signing ile yayın öncesi imzalanmalıdır. SmartScreen uyarısı oluşabilir.

Geliştirici: Bünyamin Kaya — https://bunyaminkaya.com/
