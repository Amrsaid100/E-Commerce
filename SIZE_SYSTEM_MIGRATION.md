# Migration Guide - Size System Enhancement

## Overview
This migration adds a comprehensive size management system to the e-commerce platform.

## Database Changes

### Category Table Updates
```sql
ALTER TABLE Categories 
ADD SizeType INT NOT NULL DEFAULT 0,
    MinSize INT NULL,
    MaxSize INT NULL,
    AvailableSizes NVARCHAR(200) NULL;
```

### Data Migration Examples

#### 1. Pants/Jeans Category (Numeric: 30-40)
```sql
UPDATE Categories 
SET SizeType = 1, MinSize = 30, MaxSize = 40 
WHERE Name = 'Pants' OR Name = 'Jeans' OR Name = 'بناطيل';
```

#### 2. Shoes Category (Numeric: 40-45)
```sql
UPDATE Categories 
SET SizeType = 1, MinSize = 40, MaxSize = 45 
WHERE Name = 'Shoes' OR Name = 'أحذية';
```

#### 3. Clothing Category (S, M, L, XL, XXL, XXXL)
```sql
UPDATE Categories 
SET SizeType = 2, AvailableSizes = 'S,M,L,XL,XXL,XXXL'
WHERE Name IN ('Shirts', 'T-Shirts', 'Hoodies', 'Jackets', 'قمصان', 'تيشيرتات');
```

#### 4. Accessories (No Size Required)
```sql
UPDATE Categories 
SET SizeType = 0 
WHERE Name IN ('Watches', 'Bags', 'Belts', 'ساعات', 'حقائب');
```

## EF Core Migration Command
```bash
cd "C:\Users\7450\Desktop\E-commerce FreeONe\E-Commerce"
dotnet ef migrations add AddSizeSystemToCategories
dotnet ef database update
```

## Testing Checklist
- [ ] Create new category with numeric sizes
- [ ] Create new category with clothing sizes
- [ ] Create product with size variants
- [ ] Add sized product to cart
- [ ] Complete order with sized products
- [ ] Verify admin can manage sizes
- [ ] Test size filtering in product listing
