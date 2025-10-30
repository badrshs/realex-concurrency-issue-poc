using GlobalPayments.Api;
using GlobalPayments.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RealexTraceConditionProveOfConcept.Controllers
{
    /// <summary>
    /// STANDALONE REALEX RACE CONDITION TEST
    /// Copy this entire file to prove race condition bug to Realex
    /// NO EPP DEPENDENCIES - Pure Realex SDK only
    /// </summary>
    [Route("debug/realex-race-test")]
    [AllowAnonymous]
    public class RealexRaceConditionDebugController : Controller
    {
        private readonly ILogger<RealexRaceConditionDebugController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealexRaceConditionDebugController"/> class.
        /// Realex RaceConditionDebugController
        /// </summary>
        /// <param name="logger"></param>
        public RealexRaceConditionDebugController(ILogger<RealexRaceConditionDebugController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Test UI Page
        /// GET /debug/realex-race-test
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// API endpoint - Test race condition
        /// GET /debug/realex-race-test/api
        /// </summary>
        [HttpGet("api")]
        public IActionResult TestRealexRaceCondition()
        {
            try
            {
                // HARDCODED REALEX CONFIG - Change these to your values
                const string MERCHANT_ID = "FeeFree";
                const string BASE_ACCOUNT_ID = "locale";
                const string SHARED_SECRET = "password";
                const string REFUND_PASSWORD = "Password";

                // Generate random AccountId to simulate concurrent requests
                string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 3);
                string generatedAccountId = $"{BASE_ACCOUNT_ID}_{randomSuffix}";

                _logger.LogWarning($"[RACE TEST] INPUT - MerchantId: {MERCHANT_ID}, AccountId: {generatedAccountId}");

                // Call Realex SDK directly
                string serializedJson = CallRealexSdk(
                    merchantId: MERCHANT_ID,
                    accountId: generatedAccountId,
                    sharedSecret: SHARED_SECRET,
                    refundPassword: REFUND_PASSWORD);

                // Extract BOTH merchant ID and account ID from output JSON
                string extractedMerchantId = ExtractFieldFromJson(serializedJson, "MERCHANT_ID");
                string extractedAccountId = ExtractFieldFromJson(serializedJson, "ACCOUNT");

                _logger.LogWarning($"[RACE TEST] OUTPUT - MerchantId: {extractedMerchantId}, AccountId: {extractedAccountId}");

                // VERIFY: Input vs Output for BOTH fields
                bool merchantIdsMatch = MERCHANT_ID.Equals(extractedMerchantId, StringComparison.OrdinalIgnoreCase);
                bool accountIdsMatch = generatedAccountId.Equals(extractedAccountId, StringComparison.OrdinalIgnoreCase);
                bool allMatch = merchantIdsMatch && accountIdsMatch;

                var result = new
                {
                    TestTimestamp = DateTime.UtcNow,
                    InputMerchantId = MERCHANT_ID,
                    InputAccountId = generatedAccountId,
                    OutputMerchantId = extractedMerchantId,
                    OutputAccountId = extractedAccountId,
                    MerchantIdsMatch = merchantIdsMatch,
                    AccountIdsMatch = accountIdsMatch,
                    RaceConditionDetected = !allMatch,
                    Message = allMatch
                        ? "✅ SUCCESS: Both MerchantId and AccountId match - No race condition"
                        : $"❌ RACE CONDITION DETECTED! MerchantId: {(merchantIdsMatch ? "✅" : "❌")}, AccountId: {(accountIdsMatch ? "✅" : "❌")}",
                    SerializedJsonLength = serializedJson?.Length ?? 0,
                    FullSerializedJson = serializedJson,
                };

                if (!allMatch)
                {
                    _logger.LogError($"[RACE TEST] ❌ RACE CONDITION! " +
                        $"MerchantId - Input: {MERCHANT_ID}, Output: {extractedMerchantId} | " +
                        $"AccountId - Input: {generatedAccountId}, Output: {extractedAccountId}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RACE TEST] Exception occurred");
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException?.Message,
                });
            }
        }

        /// <summary>
        /// Pure Realex SDK call - NO EPP CODE
        /// </summary>
        private string CallRealexSdk(string merchantId, string accountId, string sharedSecret, string refundPassword)
        {
            _logger.LogInformation($"[RACE TEST] Creating Realex config for: {merchantId}");

            // PURE REALEX SDK - GpEcomConfig
            var config = new GpEcomConfig
            {
                MerchantId = merchantId,
                AccountId = accountId,
                SharedSecret = sharedSecret,
                RefundPassword = refundPassword,
                RebatePassword = refundPassword,
                ServiceUrl = "https://hpp.sandbox.globalpay.com/pay", // Test URL
                HostedPaymentConfig = new HostedPaymentConfig()
                {
                    Version = "2",
                    ResponseUrl = "https://example.com/response",
                    PaymentButtonText = "Pay Now",
                },
            };

            // PURE REALEX SDK - HostedService
            HostedService hostedService = new(config, configName: merchantId);

            // PURE REALEX SDK - Generate iframe JSON
            string serialized = hostedService
                .Charge(10.00m)
                .WithCurrency("EUR")
                .Serialize(merchantId); // ← THIS IS THE KEY: What merchant comes back?

            _logger.LogWarning($"[RACE TEST] Serialize returned for: {merchantId}");

            return serialized;
        }

        /// <summary>
        /// Extract a field from Realex response JSON
        /// </summary>
        private string ExtractFieldFromJson(string serializedJson, string fieldName)
        {
            try
            {
                if (string.IsNullOrEmpty(serializedJson))
                    return "NULL_OR_EMPTY";

                _logger.LogDebug($"[RACE TEST] Attempting to parse {fieldName} from JSON: {serializedJson.Substring(0, Math.Min(100, serializedJson.Length))}...");

                // First try: Plain JSON using Realex JsonDoc parser
                try
                {
                    var jsonDoc = GlobalPayments.Api.Utils.JsonDoc.Parse(serializedJson);
                    string fieldValue = jsonDoc.GetValue<string>(fieldName);
                    _logger.LogDebug($"[RACE TEST] Successfully parsed {fieldName} from Realex JsonDoc: {fieldValue}");
                    return fieldValue ?? "NOT_FOUND_IN_JSON";
                }
                catch (Exception realexJsonEx)
                {
                    _logger.LogDebug($"[RACE TEST] Realex JsonDoc parsing failed for {fieldName}: {realexJsonEx.Message}");

                    // Second try: System.Text.Json as fallback
                    try
                    {
                        using var document = JsonDocument.Parse(serializedJson);
                        if (document.RootElement.TryGetProperty(fieldName, out JsonElement element))
                        {
                            string value = element.GetString() ?? "NULL_VALUE";
                            _logger.LogDebug($"[RACE TEST] Successfully parsed {fieldName} from System.Text.Json: {value}");
                            return value;
                        }
                        else
                        {
                            _logger.LogDebug($"[RACE TEST] Field {fieldName} not found in System.Text.Json");
                            return "NOT_FOUND_IN_JSON";
                        }
                    }
                    catch (Exception systemJsonEx)
                    {
                        _logger.LogError($"[RACE TEST] Both JSON parsing methods failed for {fieldName}. Realex: {realexJsonEx.Message}, System: {systemJsonEx.Message}");
                        return $"PARSE_ERROR: Both methods failed";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[RACE TEST] Unexpected error parsing {fieldName}");
                return $"UNEXPECTED_ERROR: {ex.Message}";
            }
        }
    }
}
