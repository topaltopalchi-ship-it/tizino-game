# ساخت اندروید و APK

## وضعیت فعلی
این مخزن کد و تنظیمات لازم برای آماده‌سازی ساخت را نگه می‌دارد؛ فایل APK فقط پس از اجرای واقعی Unity Editor با Android Build Support تولید می‌شود. وجود کد build به معنی ساخته‌شدن APK نیست.

## آماده‌سازی در Unity
1. پروژه را با Unity `6000.0.32f1` باز کنید.
2. از Unity Hub برای همین نسخه، **Android Build Support** همراه با **Android SDK & NDK Tools** و **OpenJDK** را نصب کنید.
3. منتظر بمانید Unity بسته‌ها را import و اسکریپت‌ها را compile کند؛ خطاهای Console را پیش از ساخت برطرف کنید.
4. از منوی **Tools → Tiizino → Create Stage 01 Scene** صحنهٔ اولیه را بسازید و به Build Settings اضافه کنید.
5. از **Tools → Tiizino → Validate Project Setup** برای بررسی اولیهٔ فایل‌ها و بانک‌های سؤال استفاده کنید.
6. از **Tools → Tiizino → Prepare Android Build** برای اعمال تنظیمات پیشنهادی Android استفاده کنید.
7. از **Tools → Tiizino → Build Android APK** ساخت APK را اجرا کنید.

## تنظیمات پیش‌فرض ساخت
- شناسهٔ بسته: `com.tiizino.game`
- نسخه: `0.1.0`
- جهت صفحه: عمودی (Portrait)
- حداقل Android API: 23
- معماری: ARM64
- backend اسکریپت: IL2CPP
- نوع خروجی: APK غیرامضاشدهٔ انتشار فروشگاهی نیست؛ خروجی محلی در `Builds/Android/Tiizino.apk` نوشته می‌شود.

## اگر ساخت متوقف شد
- اگر Android Build Support نصب نیست، آن را از Unity Hub برای نسخهٔ دقیق Editor نصب کنید.
- اگر صحنه وجود ندارد، ابتدا **Create Stage 01 Scene** را اجرا کنید.
- اگر صحنه در Build Settings فعال نیست، دوباره scene builder را اجرا کنید و Build Settings را بررسی کنید.
- اگر خطای کامپایل وجود دارد، ابتدا خطای نخست Console را رفع کنید؛ خطاهای بعدی ممکن است پیامد آن باشند.
- برای بررسی رفتار واقعی، APK را روی دستگاه Android نصب کنید و چک‌لیست `Docs/QA_CHECKLIST.md` را تکمیل کنید.

## انتشار
برای خروجی release و انتشار در فروشگاه، شناسهٔ نهایی، سیاست امضا، آیکون، نسخه‌بندی و تنظیمات هدف Android را بازبینی کنید. Keystore و رمزهای آن را در مخزن commit نکنید؛ آن‌ها را خارج از Git نگه دارید.

ساخت Unity در GitHub Actions به نسخهٔ دقیق Unity، لایسنس معتبر و پیکربندی build نیاز دارد. هیچ فایل لایسنس، keystore یا رمز محرمانه‌ای را در مخزن عمومی قرار ندهید.
