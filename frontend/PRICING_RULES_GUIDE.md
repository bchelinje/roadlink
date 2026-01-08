# 💰 Pricing Rules Management Guide

## Overview

The Pricing Rules feature allows administrators to create, edit, and manage dynamic pricing rules for your moving/delivery service.

## Accessing Pricing Rules

**URL**: `/admin/pricing-rules`

**Required Role**: Admin

## Features

### ✅ **View All Pricing Rules**
- See all pricing rules with their configuration
- Filter by Active/Inactive status
- View rule details including:
  - Rule type (Base Fare, Distance Based, Time Based, Surge, Service Addon)
  - Vehicle type restrictions
  - Distance and time ranges
  - Pricing amounts and rates
  - Priority and status

### ✅ **Create New Pricing Rule**
Click the **"Create New Rule"** button to open the creation form.

### ✅ **Edit Existing Rule**
Click the **"Edit"** button on any pricing rule card.

### ✅ **Toggle Active/Inactive**
Quickly activate or deactivate rules without editing them.

### ✅ **Delete Pricing Rule**
Remove pricing rules that are no longer needed.

---

## Pricing Rule Configuration

### **1. Basic Information**

#### Rule Name (Required)
- **Example**: "Base Fare - Standard Van"
- Clear, descriptive name for the rule

#### Description (Optional)
- Additional context about when/why this rule applies
- **Example**: "Weekend premium for cargo vans in downtown area"

#### Rule Type (Required)
Choose from:
- **BaseFare**: Fixed starting price
- **DistanceBased**: Pricing based on miles traveled
- **TimeBased**: Pricing based on time of day
- **Surge**: Dynamic pricing multipliers
- **ServiceAddon**: Additional services (helpers, packing, etc.)

#### Vehicle Type (Optional)
Apply rule to specific vehicle types:
- All Vehicles (default)
- Van
- Cargo Van
- Small Truck
- Large Truck
- Box Truck

---

### **2. Pricing Configuration**

#### Fixed Amount ($)
- One-time flat fee
- **Example**: $50 base fare
- Used for: Base fares, flat service fees

#### Per Mile Rate ($)
- Cost per mile traveled
- **Example**: $2.50 per mile
- Used for: Distance-based pricing

#### Multiplier (%)
- Percentage multiplier applied to base price
- **Example**: 150% for weekend surge pricing
- Used for: Surge pricing, peak hour pricing

---

### **3. Distance Range (Optional)**

Apply this rule only within certain distance ranges:

- **Min Distance**: Minimum miles for rule to apply (e.g., 5 miles)
- **Max Distance**: Maximum miles for rule to apply (e.g., 50 miles)
- Leave empty for no restriction

**Example**:
- Long distance rule: Min 50 miles, Max unlimited
- Short haul rule: Min 0 miles, Max 10 miles

---

### **4. Time Range (Optional)**

Apply this rule only during specific times:

- **Start Time**: When rule begins (e.g., 17:00)
- **End Time**: When rule ends (e.g., 21:00)

**Example**:
- Evening rush hour: 17:00 - 21:00
- Late night premium: 22:00 - 06:00

---

### **5. Day Filters**

#### Weekend Only
- ☑️ Apply this rule only on Saturdays and Sundays

#### Weekday Only
- ☑️ Apply this rule only Monday through Friday

**Note**: Don't check both boxes - they're mutually exclusive!

---

### **6. Priority & Status**

#### Priority
- **Number**: Higher numbers = higher priority
- **Default**: 0
- Rules with higher priority are applied first
- **Example**:
  - Base fare: Priority 1
  - Distance rate: Priority 2
  - Weekend surge: Priority 3

#### Active
- ☑️ Rule is currently active and will be used in pricing calculations
- ☐ Rule is inactive and will be ignored

---

## Example Pricing Rules

### Example 1: Base Fare for All Vehicles
```
Name: Standard Base Fare
Type: BaseFare
Vehicle Type: (All)
Fixed Amount: $30.00
Priority: 1
Active: ✓
```

### Example 2: Distance-Based Pricing
```
Name: Per Mile Rate - Cargo Van
Type: DistanceBased
Vehicle Type: Cargo Van
Per Mile Rate: $2.50
Distance Range: 0 - 100 miles
Priority: 2
Active: ✓
```

### Example 3: Weekend Surge Pricing
```
Name: Weekend Premium
Type: Surge
Multiplier: 120%
Weekend Only: ✓
Priority: 5
Active: ✓
```

### Example 4: Rush Hour Pricing
```
Name: Evening Rush Hour
Type: TimeBased
Time Range: 17:00 - 21:00
Multiplier: 115%
Weekday Only: ✓
Priority: 4
Active: ✓
```

### Example 5: Long Distance Discount
```
Name: Long Haul Discount
Type: DistanceBased
Per Mile Rate: $1.80
Distance Range: 100+ miles
Priority: 3
Active: ✓
```

---

## How Pricing Rules Work Together

1. **Multiple Rules Apply**: Multiple rules can apply to a single job
2. **Priority Order**: Rules are applied in priority order (highest first)
3. **Cumulative**: Pricing calculations combine applicable rules
4. **Filters**: Only rules matching all conditions apply:
   - Vehicle type matches (or rule applies to all)
   - Distance is within range (if specified)
   - Time is within range (if specified)
   - Day of week matches (if specified)

### Calculation Example

**Job Details**:
- Vehicle: Cargo Van
- Distance: 15 miles
- Time: Saturday, 18:00

**Applicable Rules**:
1. Base Fare: $30 (Priority 1)
2. Per Mile Rate: $2.50/mile (Priority 2)
3. Weekend Premium: 120% (Priority 5)

**Calculation**:
```
Base = $30
Distance = 15 miles × $2.50 = $37.50
Subtotal = $30 + $37.50 = $67.50
Weekend Multiplier = $67.50 × 1.20 = $81.00
Final Price = $81.00
```

---

## Best Practices

### ✅ **DO**
- Use clear, descriptive names
- Set appropriate priorities (base fares lower, surges higher)
- Test new rules before activating
- Use inactive status for seasonal rules
- Document complex rules in the description field

### ❌ **DON'T**
- Create conflicting rules (e.g., overlapping distance ranges with different rates)
- Set both "Weekend Only" and "Weekday Only"
- Use extremely high multipliers without warning customers
- Delete rules that are referenced in historical jobs
- Forget to set priority on rules

---

## Troubleshooting

### Pricing seems incorrect
1. Check all active rules applying to the job
2. Verify priority order is correct
3. Look for unintended rule overlaps
4. Check time/day filters match expectations

### Rule not applying
1. Verify rule is **Active**
2. Check vehicle type filter
3. Verify distance range includes job distance
4. Confirm time range includes job time
5. Check day filters (weekend/weekday)

### Multiple similar rules
1. Use priority to control which applies first
2. Use distance/time ranges to separate rules
3. Consider using vehicle type to differentiate

---

## Access Control

- **Admin Only**: Only users with Admin role can access pricing rules
- All changes are logged with creator/modifier information
- Rules can be activated/deactivated without deletion

---

## Questions?

For issues or questions:
1. Check the rule configuration
2. Review priority and filters
3. Test with sample job parameters
4. Contact system administrator if issues persist

---

**Pro Tip**: Create a "Test Rule" with high priority and specific filters to test pricing calculations before rolling out new pricing strategies!
