using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MoneyBoard.Application.DTOs;
using MoneyBoard.Application.Interfaces;
using MoneyBoard.Application.Services;
using MoneyBoard.Domain.Entities;
using MoneyBoard.Domain.Repositories;
using Xunit;

namespace MoneyBoard.Application.Tests
{
    public class RepaymentServiceTests
    {
        private readonly Mock<IRepaymentRepository> _repaymentRepositoryMock;
        private readonly Mock<ILoanRepository> _loanRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<RepaymentService>> _loggerMock;
        private readonly RepaymentService _service;

        public RepaymentServiceTests()
        {
            _repaymentRepositoryMock = new Mock<IRepaymentRepository>();
            _loanRepositoryMock = new Mock<ILoanRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<RepaymentService>>();

            _service = new RepaymentService(
                _repaymentRepositoryMock.Object,
                _loanRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CreateRepaymentAsync_ValidRequest_ReturnsRepaymentResponse()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow,
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));

            var nextDueDate = DateTime.UtcNow.AddMonths(1);
            var repayment = new Repayment(loanId, request.Amount, request.RepaymentDate, 0, 0, nextDueDate);
            var response = new RepaymentResponseDto { Id = repayment.Id, Amount = request.Amount };

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.AddRepaymentAsync(It.IsAny<Repayment>())).Returns(Task.CompletedTask);
            _loanRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Loan>())).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mapperMock.Setup(m => m.Map<RepaymentResponseDto>(It.IsAny<Repayment>())).Returns(response);

            // Act
            var result = await _service.CreateRepaymentAsync(loanId, request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Amount, result.Amount);
            _repaymentRepositoryMock.Verify(r => r.AddRepaymentAsync(It.IsAny<Repayment>()), Times.Once);
            _loanRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Once);
        }

        [Fact]
        public async Task CreateRepaymentAsync_OverpaymentNotAllowed_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 10000, // More than balance
                RepaymentDate = DateTime.UtcNow
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));
            loan.AllowOverpayment = false; // Overpayment not allowed

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateRepaymentAsync(loanId, request, userId));
            Assert.Contains("OVERPAYMENT_NOT_ALLOWED", exception.Message);
        }

        [Fact]
        public async Task CreateRepaymentAsync_LoanNotFound_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto { Amount = 1000, RepaymentDate = DateTime.UtcNow };

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync((Loan?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.CreateRepaymentAsync(loanId, request, userId));
        }

        [Fact]
        public async Task GetRepaymentsAsync_ValidRequest_ReturnsPagedResponse()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var repayments = new List<Repayment>
            {
                new Repayment(loanId, 1000, DateTime.UtcNow, 0, 0, DateTime.UtcNow.AddMonths(1))
            };
            var repaymentDtos = new List<RepaymentDto>
            {
                new RepaymentDto { Id = repayments[0].Id, Amount = 1000 }
            };

            var loan = new Loan(userId, "Test", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentsByLoanIdAsync(loanId, 1, 25, null, null)).ReturnsAsync(repayments);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentCountAsync(loanId, null)).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<IEnumerable<RepaymentDto>>(repayments)).Returns(repaymentDtos);

            // Act
            var result = await _service.GetRepaymentsAsync(loanId, 1, 25, null, null, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Repayments);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task CreateRepaymentAsync_DuplicateMonthlyRepayment_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var repaymentDate = new DateTime(2024, 9, 15, 10, 0, 0, DateTimeKind.Utc);

            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = repaymentDate,
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));
            loan.RepaymentFrequency = Domain.Enums.RepaymentFrequencyType.Monthly;

            // Mock existing repayment in the same month
            var existingRepayments = new List<Repayment>
            {
                new Repayment(loanId, 1000, repaymentDate.AddDays(-5), 0, 0, repaymentDate.AddMonths(1)) // Same month
            };

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentsByLoanIdAsync(loanId, 1, int.MaxValue, null, null)).ReturnsAsync(existingRepayments);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateRepaymentAsync(loanId, request, userId));
            Assert.Contains("monthly period", exception.Message);
        }

        [Fact]
        public async Task CreateRepaymentAsync_CompletedLoan_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow,
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));
            loan.Status = Domain.Enums.LoanStatus.Completed; // Loan is completed

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateRepaymentAsync(loanId, request, userId));
            Assert.Contains("completed loan", exception.Message);
        }

        [Fact]
        public async Task CreateRepaymentAsync_RepaymentDateTooFarInFuture_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow.AddYears(2), // More than 1 year in future
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentsByLoanIdAsync(loanId, 1, int.MaxValue, null, null)).ReturnsAsync(new List<Repayment>());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateRepaymentAsync(loanId, request, userId));
            Assert.Contains("1 year in the future", exception.Message);
        }

        [Fact]
        public async Task CreateRepaymentAsync_LumpSumWithExistingRepayment_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow,
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));
            loan.RepaymentFrequency = Domain.Enums.RepaymentFrequencyType.LumpSum;

            // Mock existing repayment for lump sum
            var existingRepayments = new List<Repayment>
            {
                new Repayment(loanId, 1000, DateTime.UtcNow.AddDays(-30), 0, 0, DateTime.UtcNow.AddMonths(1))
            };

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentsByLoanIdAsync(loanId, 1, int.MaxValue, null, null)).ReturnsAsync(existingRepayments);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateRepaymentAsync(loanId, request, userId));
            Assert.Contains("lump sum period", exception.Message);
        }

        [Fact]
        public async Task UpdateRepaymentAsync_DuplicateMonthlyRepayment_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var repaymentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var repaymentDate = new DateTime(2024, 9, 15, 10, 0, 0, DateTimeKind.Utc);

            var request = new UpdateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = repaymentDate,
                Notes = "Updated repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow));
            loan.RepaymentFrequency = Domain.Enums.RepaymentFrequencyType.Monthly;

            var existingRepayment = new Repayment(loanId, 500, repaymentDate.AddDays(-10), 0, 0, repaymentDate.AddMonths(1));

            // Mock existing repayment in the same month (different from the one being updated)
            var existingRepayments = new List<Repayment>
            {
                new Repayment(loanId, 1000, repaymentDate.AddDays(-5), 0, 0, repaymentDate.AddMonths(1)), // Same month, different repayment
                existingRepayment
            };

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetByIdAsync(repaymentId)).ReturnsAsync(existingRepayment);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentsByLoanIdAsync(loanId, 1, int.MaxValue, null, null)).ReturnsAsync(existingRepayments);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateRepaymentAsync(loanId, repaymentId, request, userId));
            Assert.Contains("monthly period", exception.Message);
        }

        [Fact]
        public async Task CreateRepaymentAsync_ExceedsExpectedInstallments_AllowOverpaymentFalse_ReturnsError()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow,
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-5)));
            loan.EndDate = DateOnly.FromDateTime(DateTime.UtcNow); // 5 months loan
            loan.RepaymentFrequency = Domain.Enums.RepaymentFrequencyType.Monthly;
            loan.AllowOverpayment = false;

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentCountAsync(loanId, null)).ReturnsAsync(5); // 5 existing, expected 5

            // Act
            var result = await _service.CreateRepaymentAsync(loanId, request, userId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("exceeds expected schedule", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateRepaymentAsync_ExceedsExpectedInstallments_AllowOverpaymentTrue_Succeeds()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow,
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-5)));
            loan.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            loan.RepaymentFrequency = Domain.Enums.RepaymentFrequencyType.Monthly;
            loan.AllowOverpayment = true; // Allow overpayment

            var nextDueDate = DateTime.UtcNow.AddMonths(1);
            var repayment = new Repayment(loanId, request.Amount, request.RepaymentDate, 0, 0, nextDueDate);
            var response = new RepaymentResponseDto { Id = repayment.Id, Amount = request.Amount };

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentCountAsync(loanId, null)).ReturnsAsync(5);
            _repaymentRepositoryMock.Setup(r => r.AddRepaymentAsync(It.IsAny<Repayment>())).Returns(Task.CompletedTask);
            _loanRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Loan>())).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mapperMock.Setup(m => m.Map<RepaymentResponseDto>(It.IsAny<Repayment>())).Returns(response);

            // Act
            var result = await _service.CreateRepaymentAsync(loanId, request, userId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CreateRepaymentAsync_RepaymentDateAfterEndDate_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow.AddDays(40), // After end +30 days grace
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)));
            loan.EndDate = DateOnly.FromDateTime(DateTime.UtcNow); // End date now
            loan.AllowOverpayment = true;

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentCountAsync(loanId, null)).ReturnsAsync(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateRepaymentAsync(loanId, request, userId));
            Assert.Contains("outside the loan's valid period", exception.Message);
        }

        [Fact]
        public async Task CreateRepaymentAsync_RepaymentDateWithinGracePeriod_Succeeds()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow.AddDays(20), // Within 30 days grace
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)));
            loan.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            loan.AllowOverpayment = true;

            var nextDueDate = DateTime.UtcNow.AddMonths(1);
            var repayment = new Repayment(loanId, request.Amount, request.RepaymentDate, 0, 0, nextDueDate);
            var response = new RepaymentResponseDto { Id = repayment.Id, Amount = request.Amount };

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentCountAsync(loanId, null)).ReturnsAsync(0);
            _repaymentRepositoryMock.Setup(r => r.AddRepaymentAsync(It.IsAny<Repayment>())).Returns(Task.CompletedTask);
            _loanRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Loan>())).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mapperMock.Setup(m => m.Map<RepaymentResponseDto>(It.IsAny<Repayment>())).Returns(response);

            // Act
            var result = await _service.CreateRepaymentAsync(loanId, request, userId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CreateRepaymentAsync_LumpSumExceedsExpected_ThrowsException()
        {
            // Arrange
            var loanId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new CreateRepaymentRequestDto
            {
                Amount = 1000,
                RepaymentDate = DateTime.UtcNow,
                Notes = "Test repayment"
            };

            var loan = new Loan(userId, "Test Counterparty", 5000, 5, Domain.Enums.InterestType.Simple, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)));
            loan.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            loan.RepaymentFrequency = Domain.Enums.RepaymentFrequencyType.LumpSum;
            loan.AllowOverpayment = false;

            _loanRepositoryMock.Setup(r => r.GetByIdAsync(loanId)).ReturnsAsync(loan);
            _repaymentRepositoryMock.Setup(r => r.GetRepaymentCountAsync(loanId, null)).ReturnsAsync(1); // Already 1, expected 1

            // Act
            var result = await _service.CreateRepaymentAsync(loanId, request, userId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("exceeds expected schedule", result.ErrorMessage);
        }
    }
}
