# ساخت اندروید و APK

## وضعیت فعلی
این مخزن هنوز پروژهٔ کامل Unity نیست و APK تولید نشده است. مستندات و نمونه‌کد به‌تنهایی APK قابل نصب نمی‌سازند.

## نیازمندی‌ها
- Unity 6 Editor سازگار با URP.
- Android Build Support، Android SDK & NDK Tools و OpenJDK از طریق Unity Hub.
- صحنه‌ها، منابع و تنظیمات Android پروژه.
- شناسهٔ بستهٔ نهایی، برای نمونه com.tiizino.game.
- کلید امضای release باید خارج از Git نگهداری شود.

## ساخت محلی
۱. پروژه را در Unity باز کنید.
۲. در Build Profiles، Android را انتخاب کنید.
۳. صحنه‌های لازم را به build اضافه کنید.
۴. Package Identifier و معماری‌های CPU را تنظیم کنید.
۵. Development Build را روی دستگاه واقعی آزمایش کنید.
۶. پس از تست، خروجی release امضاشده بسازید.

## CI
ساخت Unity در GitHub Actions به نسخهٔ Unity، لایسنس و پیکربندی build نیاز دارد. فایل لایسنس، keystore یا رمزها را در مخزن عمومی commit نکنید.
