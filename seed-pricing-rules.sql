-- Seed default pricing rules for BeC delivery platform
-- Run this SQL script against your database to set up pricing

-- Clear existing pricing rules (optional)
-- DELETE FROM PricingRules;

-- 1. Base Fare
INSERT INTO PricingRules (Id, Name, Type, IsActive, Priority, FixedAmount, CreatedAt, UpdatedAt)
VALUES
(NEWID(), 'Standard Base Fare', 'base_fare', 1, 1, 15.00, GETDATE(), GETDATE());

-- 2. Per Mile Rates (by vehicle type)
INSERT INTO PricingRules (Id, Name, Type, IsActive, Priority, PerMileRate, VehicleType, CreatedAt, UpdatedAt)
VALUES
(NEWID(), 'Van - Per Mile Rate', 'per_mile', 1, 2, 2.50, 'van', GETDATE(), GETDATE()),
(NEWID(), 'Small Truck - Per Mile Rate', 'per_mile', 1, 2, 3.00, 'small_truck', GETDATE(), GETDATE()),
(NEWID(), 'Large Truck - Per Mile Rate', 'per_mile', 1, 2, 4.00, 'large_truck', GETDATE(), GETDATE());

-- 3. Service Add-ons
INSERT INTO PricingRules (Id, Name, Type, ServiceAddonType, IsActive, Priority, FixedAmount, CreatedAt, UpdatedAt)
VALUES
(NEWID(), 'Additional Helper', 'service_addon', 'helper', 1, 3, 25.00, GETDATE(), GETDATE()),
(NEWID(), 'Floor Charge (No Elevator)', 'service_addon', 'floor_charge', 1, 3, 10.00, GETDATE(), GETDATE()),
(NEWID(), 'Stairs Charge', 'service_addon', 'stairs_charge', 1, 3, 5.00, GETDATE(), GETDATE()),
(NEWID(), 'Waiting Time', 'service_addon', 'waiting_time', 1, 3, 0.50, GETDATE(), GETDATE());

-- 4. Time-based Multipliers (Surge Pricing)
INSERT INTO PricingRules (Id, Name, Type, IsActive, Priority, MultiplierPercentage, WeekendOnly, StartTime, EndTime, CreatedAt, UpdatedAt)
VALUES
(NEWID(), 'Weekend Surge', 'time_multiplier', 1, 4, 1.25, 1, NULL, NULL, GETDATE(), GETDATE()),
(NEWID(), 'Peak Hours (7-9 AM)', 'time_multiplier', 1, 4, 1.20, 0, '07:00:00', '09:00:00', GETDATE(), GETDATE()),
(NEWID(), 'Peak Hours (5-7 PM)', 'time_multiplier', 1, 4, 1.20, 0, '17:00:00', '19:00:00', GETDATE(), GETDATE());

-- Verify the rules were inserted
SELECT * FROM PricingRules ORDER BY Priority, Type;
