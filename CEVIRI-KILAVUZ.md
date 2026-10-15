# Yeni arayüz dili eklemek
Türkçe kaynak: WindowsISODownloader/Strings.resx; İngilizce: Strings.en.resx. Anahtarlar eşleşmelidir. Metinlerin çevirisini yeni Strings.<kültür>.resx dosyasına ekleyin. ResourceManager kültür seçimini ve geri dönüşü yapar.

App.SetLanguage içindeki desteklenen kültürlere, MainWindow.xaml UiLanguageCombo seçeneklerine ve CLI --ui-lang doğrulamasına yeni kültürü ekleyin. Kaydedilmiş dili okuyan App.OnStartup kodunu da yeni kültürü kabul edecek şekilde güncelleyin. Böylece metin çağrıları yeniden tr/en çiftleriyle yazılmaz; App.T(anahtar) korunur. Teknik ürün/kanal/mimari kimliklerini çevirmeyin.

Kurulum.ps1 .resx dosyalarını otomatik derleme alanına taşır. Tek EXE yayınında kaynaklar paketlenir. Bu sürümde yalnızca Türkçe ve İngilizce çeviri sağlandı.
