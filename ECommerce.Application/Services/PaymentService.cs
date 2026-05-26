using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ECommerce.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private const string PendingPaymentStatus = "PendingPayment";
        private const string PaidStatus = "Paid";
        private const string FailedStatus = "Failed";

        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly HttpClient _httpClient;
        private readonly RazorpayOptions _razorpayOptions;

        public PaymentService(
            ICartRepository cartRepository,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IPaymentRepository paymentRepository,
            HttpClient httpClient,
            IOptions<RazorpayOptions> razorpayOptions)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _httpClient = httpClient;
            _razorpayOptions = razorpayOptions.Value;
        }

        public async Task<CreatePaymentOrderResponseDto> CreateOrderAsync(string username)
        {
            var localOrder = await CreateLocalOrderAsync(username);

            try
            {
                var razorpayOrder = await CreateRazorpayOrderAsync(localOrder);

                return new CreatePaymentOrderResponseDto
                {
                    KeyId = _razorpayOptions.KeyId,
                    RazorpayOrderId = razorpayOrder.RazorpayOrderId,
                    Amount = razorpayOrder.Amount,
                    LocalOrderId = localOrder.Id
                };
            }
            catch
            {
                localOrder.Status = FailedStatus;
                await _paymentRepository.UpdateOrderAsync(localOrder);
                throw;
            }
        }

        public async Task<Order> CreateLocalOrderAsync(string username)
        {
            var user = _userRepository.GetByUsername(username);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Authenticated user was not found.");
            }

            var cartItems = (await _cartRepository.GetCart(username)).ToList();

            if (cartItems.Count == 0)
            {
                throw new ValidationException("Your cart is empty.");
            }

            await _productRepository.ValidateCartStockAsync(cartItems);

            var amount = cartItems.Sum(item => item.Product.Price * item.Quantity);

            if (amount <= 0)
            {
                throw new ValidationException("Invalid cart total.");
            }

            var order = new Order
            {
                UserId = user.Id,
                Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
                Status = PendingPaymentStatus,
                CreatedAt = DateTime.UtcNow
            };

            return await _paymentRepository.AddOrderAsync(order);
        }

        public async Task<RazorpayOrderResultDto> CreateRazorpayOrderAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(_razorpayOptions.KeyId) || string.IsNullOrWhiteSpace(_razorpayOptions.KeySecret))
            {
                throw new InvalidOperationException("Razorpay credentials are not configured.");
            }

            var amountInPaise = Convert.ToInt64(decimal.Round(order.Amount * 100, 0, MidpointRounding.AwayFromZero));
            var payload = JsonSerializer.Serialize(new
            {
                amount = amountInPaise,
                currency = "INR",
                receipt = $"order_{order.Id}"
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_razorpayOptions.KeyId}:{_razorpayOptions.KeySecret}")));
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Unable to create Razorpay order.");
            }

            using var document = JsonDocument.Parse(responseContent);
            var razorpayOrderId = document.RootElement.GetProperty("id").GetString();

            if (string.IsNullOrWhiteSpace(razorpayOrderId))
            {
                throw new InvalidOperationException("Razorpay order id was not returned.");
            }

            order.RazorpayOrderId = razorpayOrderId;
            await _paymentRepository.UpdateOrderAsync(order);

            return new RazorpayOrderResultDto
            {
                RazorpayOrderId = razorpayOrderId,
                Amount = amountInPaise
            };
        }

        public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
        {
            if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            var payload = $"{orderId}|{paymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_razorpayOptions.KeySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var generatedSignature = Convert.ToHexString(hash).ToLowerInvariant();

            return string.Equals(generatedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<PaymentResultDto> FinalizePaymentAsync(string username, VerifyPaymentDto dto)
        {
            var user = _userRepository.GetByUsername(username);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Authenticated user was not found.");
            }

            var order = await _paymentRepository.GetOrderByIdAsync(dto.LocalOrderId);

            if (order == null)
            {
                throw new ValidationException("Order not found.");
            }

            if (order.UserId != user.Id)
            {
                throw new UnauthorizedAccessException("This order does not belong to the current user.");
            }

            if (!string.Equals(order.RazorpayOrderId, dto.RazorpayOrderId, StringComparison.Ordinal))
            {
                order.Status = FailedStatus;
                await _paymentRepository.UpdateOrderAsync(order);

                return new PaymentResultDto
                {
                    Success = false,
                    Status = FailedStatus,
                    Message = "Order verification failed."
                };
            }

            var existingPayment = await _paymentRepository.GetPaymentByRazorpayPaymentIdAsync(dto.RazorpayPaymentId);
            if (existingPayment != null && string.Equals(existingPayment.Status, PaidStatus, StringComparison.OrdinalIgnoreCase))
            {
                return new PaymentResultDto
                {
                    Success = true,
                    Status = PaidStatus,
                    Message = "Payment already verified."
                };
            }

            var isValid = VerifyPaymentSignature(dto.RazorpayOrderId, dto.RazorpayPaymentId, dto.RazorpaySignature);

            if (!isValid)
            {
                order.Status = FailedStatus;
                await _paymentRepository.UpdateOrderAsync(order);
                await SavePaymentAsync(existingPayment, order, dto, FailedStatus);

                return new PaymentResultDto
                {
                    Success = false,
                    Status = FailedStatus,
                    Message = "Payment signature verification failed."
                };
            }

            try
            {
                var cartItems = (await _cartRepository.GetCart(username)).ToList();
                await _productRepository.ReduceStockForCartAsync(cartItems);
                order.Status = PaidStatus;
                await _paymentRepository.UpdateOrderAsync(order);
                await SavePaymentAsync(existingPayment, order, dto, PaidStatus);
                await _cartRepository.ClearCart(username);
            }
            catch (ValidationException)
            {
                order.Status = FailedStatus;
                await _paymentRepository.UpdateOrderAsync(order);
                await SavePaymentAsync(existingPayment, order, dto, FailedStatus);
                throw;
            }

            return new PaymentResultDto
            {
                Success = true,
                Status = PaidStatus,
                Message = "Payment verified successfully."
            };
        }

        private async Task SavePaymentAsync(Payment? existingPayment, Order order, VerifyPaymentDto dto, string status)
        {
            if (existingPayment == null)
            {
                await _paymentRepository.AddPaymentAsync(new Payment
                {
                    OrderId = order.Id,
                    RazorpayPaymentId = dto.RazorpayPaymentId,
                    RazorpayOrderId = dto.RazorpayOrderId,
                    Amount = order.Amount,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                });
                return;
            }

            existingPayment.Status = status;
            existingPayment.RazorpayOrderId = dto.RazorpayOrderId;
            existingPayment.Amount = order.Amount;
            await _paymentRepository.UpdatePaymentAsync(existingPayment);
        }
    }
}
