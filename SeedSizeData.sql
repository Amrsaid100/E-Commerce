-- إضافة بيانات تجريبية لنظام المقاسات
-- تأكد من تشغيل هذا السكريبت بعد عمل Migration

USE [ecommerce]
GO

-- 1. تحديث الفئات الموجودة أو إضافة فئات جديدة

-- فئة البناطيل (مقاسات رقمية 30-40)
IF EXISTS (SELECT 1 FROM Categories WHERE Name = 'Pants' OR Name = 'بناطيل')
BEGIN
    UPDATE Categories 
    SET SizeType = 1, MinSize = 30, MaxSize = 40, AvailableSizes = NULL
    WHERE Name IN ('Pants', 'بناطيل');
    PRINT '✅ تم تحديث فئة البناطيل';
END
ELSE
BEGIN
    INSERT INTO Categories (Name, Description, SizeType, MinSize, MaxSize, AvailableSizes)
    VALUES ('بناطيل', 'بناطيل جينز ورسمية بمقاسات 30-40', 1, 30, 40, NULL);
    PRINT '✅ تم إضافة فئة البناطيل';
END

-- فئة الأحذية (مقاسات رقمية 40-45)
IF EXISTS (SELECT 1 FROM Categories WHERE Name = 'Shoes' OR Name = 'أحذية')
BEGIN
    UPDATE Categories 
    SET SizeType = 1, MinSize = 40, MaxSize = 45, AvailableSizes = NULL
    WHERE Name IN ('Shoes', 'أحذية');
    PRINT '✅ تم تحديث فئة الأحذية';
END
ELSE
BEGIN
    INSERT INTO Categories (Name, Description, SizeType, MinSize, MaxSize, AvailableSizes)
    VALUES ('أحذية', 'أحذية رياضية وكاجوال بمقاسات 40-45', 1, 40, 45, NULL);
    PRINT '✅ تم إضافة فئة الأحذية';
END

-- فئة التيشيرتات (مقاسات S-XXXL)
IF EXISTS (SELECT 1 FROM Categories WHERE Name = 'T-Shirts' OR Name = 'تيشيرتات')
BEGIN
    UPDATE Categories 
    SET SizeType = 2, MinSize = NULL, MaxSize = NULL, AvailableSizes = 'S,M,L,XL,XXL,XXXL'
    WHERE Name IN ('T-Shirts', 'تيشيرتات');
    PRINT '✅ تم تحديث فئة التيشيرتات';
END
ELSE
BEGIN
    INSERT INTO Categories (Name, Description, SizeType, MinSize, MaxSize, AvailableSizes)
    VALUES ('تيشيرتات', 'تيشيرتات قطن ورياضية بمقاسات S-XXXL', 2, NULL, NULL, 'S,M,L,XL,XXL,XXXL');
    PRINT '✅ تم إضافة فئة التيشيرتات';
END

-- فئة القمصان (مقاسات S-XXXL)
IF EXISTS (SELECT 1 FROM Categories WHERE Name = 'Shirts' OR Name = 'قمصان')
BEGIN
    UPDATE Categories 
    SET SizeType = 2, MinSize = NULL, MaxSize = NULL, AvailableSizes = 'S,M,L,XL,XXL,XXXL'
    WHERE Name IN ('Shirts', 'قمصان');
    PRINT '✅ تم تحديث فئة القمصان';
END
ELSE
BEGIN
    INSERT INTO Categories (Name, Description, SizeType, MinSize, MaxSize, AvailableSizes)
    VALUES ('قمصان', 'قمصان رسمية وكاجوال بمقاسات S-XXXL', 2, NULL, NULL, 'S,M,L,XL,XXL,XXXL');
    PRINT '✅ تم إضافة فئة القمصان';
END

-- فئة الساعات (بدون مقاسات)
IF EXISTS (SELECT 1 FROM Categories WHERE Name = 'Watches' OR Name = 'ساعات')
BEGIN
    UPDATE Categories 
    SET SizeType = 0, MinSize = NULL, MaxSize = NULL, AvailableSizes = NULL
    WHERE Name IN ('Watches', 'ساعات');
    PRINT '✅ تم تحديث فئة الساعات';
END
ELSE
BEGIN
    INSERT INTO Categories (Name, Description, SizeType, MinSize, MaxSize, AvailableSizes)
    VALUES ('ساعات', 'ساعات يد رجالية ونسائية', 0, NULL, NULL, NULL);
    PRINT '✅ تم إضافة فئة الساعات';
END

-- فئة الحقائب (بدون مقاسات)
IF EXISTS (SELECT 1 FROM Categories WHERE Name = 'Bags' OR Name = 'حقائب')
BEGIN
    UPDATE Categories 
    SET SizeType = 0, MinSize = NULL, MaxSize = NULL, AvailableSizes = NULL
    WHERE Name IN ('Bags', 'حقائب');
    PRINT '✅ تم تحديث فئة الحقائب';
END
ELSE
BEGIN
    INSERT INTO Categories (Name, Description, SizeType, MinSize, MaxSize, AvailableSizes)
    VALUES ('حقائب', 'حقائب يد وظهر', 0, NULL, NULL, NULL);
    PRINT '✅ تم إضافة فئة الحقائب';
END

PRINT '🎉 تم إضافة/تحديث جميع الفئات بنجاح!';
GO
