# R18 Store settings

Store reservation policy lives on the existing Admin Settings «مهلت‌ها» tab.

Section: سیاست رزرو موجودی

Fields: مدت رزرو اولیه / مدت رزرو مجدد / حداکثر دفعات رزرو

Empty inherit checkbox = platform default. Save/Cancel are the existing hold-policy buttons. Dedicated `/v1/admin/settings/reservation-policy/store` also exists for focused writes.

Help states: initial starts at payment; retry only after previous ended + reacquire; failed payment does not reset timer; max includes initial.
