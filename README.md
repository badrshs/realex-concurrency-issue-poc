# Realex Race Condition Test - Standalone Version

[Download the video](https://github.com/badrshs/realex-concurrency-issue-poc/raw/refs/heads/main/video-record.mp4)

![chrome_p7uITDGFHn](https://github.com/user-attachments/assets/7530cc75-b8c4-444e-be05-1713c8b20c27)

## Suggestions
Please use **Firefox Mozilla** or  use Chrome but with the debug tools open.
We are able to reproduce this on every click that has two simulated requests. If you are not able to reproduce it, try a larger number.

## 🎯 Purpose
This is a **standalone, copy-paste ready** test to reproduce and prove a race condition bug in the Realex/Global Payments SDK. When multiple concurrent requests are made with different `AccountId` values, the SDK sometimes returns the wrong account configuration in the serialized JSON response.

## 📦 What's Included
This standalone version uses only pure Realex SDK (`GlobalPayments.Api`).

### Files
1. **`RealexRaceConditionDebugController.cs`** - Controller with pure Realex SDK code
2. **`Views/RealexRaceConditionDebug/Index.cshtml`** - Beautiful UI for concurrent testing

### The Test
1. **Static MerchantId**: Always uses the same value (e.g., `"Feepay2"`)
2. **Random AccountId**: Generates unique values for each request (e.g., `"locale_a3f"`, `"locale_b7e"`)
3. **Concurrent Requests**: Fires multiple requests simultaneously
4. **Verification**: Compares input values vs. output values in serialized JSON

### Race Condition Detection
The bug occurs when:
- **Input AccountId**: `locale_a3f`
- **Output AccountId**: `locale_b7e` ❌ **MISMATCH!**

This proves that concurrent requests are sharing/mixing configurations incorrectly.

### Real-Time Stats
- **Total Requests**: Number of completed requests
- **✅ Passed**: Requests where input = output (no race condition)
- **⚠️ Race Conditions**: Requests where input ≠ output (BUG DETECTED!)
- **❌ Errors**: Failed requests

### Results Display
Each result shows:
- Request number
- Status badge (PASSED, RACE CONDITION, ERROR)
- Input vs Output comparison for both MerchantId and AccountId
- Color-coded highlighting for race conditions (orange)
- Timestamp

## 📊 Expected Results

### ✅ No Race Condition (Ideal)
```
Input AccountId:  locale_a3f
Output AccountId: locale_a3f
Status: PASSED
```

### ❌ Race Condition Detected (Bug!)
```
Input AccountId:  locale_a3f
Output AccountId: locale_b7e
Status: RACE CONDITION DETECTED!
```

## 🐛 Reproducing the Bug

### Best Practices
1. **High Concurrency**: Use 1000 simultaneous requests
2. **No Delay**: Set delay to 0ms for maximum concurrency
3. **Multiple Runs**: Run the test several times - race conditions are intermittent
4. **Watch for Orange Cards**: Race conditions are highlighted in orange
 
## 🎯 Success Criteria

**The test is working when:**
- All requests pass (green) = No race condition
- Some requests show orange = Race condition detected! 🎉 (Bug reproduced!)
---

**Created for**: Proving Realex SDK thread-safety bug  
