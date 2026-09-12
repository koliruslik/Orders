using Microsoft.AspNetCore.Mvc;
using Northwind.Orders.WebApi.Models;
using Northwind.Services.Repositories;

namespace Northwind.Orders.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderRepository repository;
    private readonly ILogger<OrdersController> logger;

    public OrdersController(IOrderRepository orderRepository, ILogger<OrdersController> logger)
    {
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(logger);

        this.repository = orderRepository;
        this.logger = logger;
    }

    [HttpGet("{orderId:long}")]
    public async Task<ActionResult<FullOrder>> GetOrderAsync(long orderId)
    {
        try
        {
            this.logger.LogTrace(
                "Getting order with ID {OrderId}.",
                orderId);

            var order = await this.repository.GetOrderAsync(orderId);

            var result = new FullOrder
            {
                Id = order.Id,

                Customer = new Models.Customer
                {
                    Code = order.Customer.Code.Code,
                    CompanyName = order.Customer.CompanyName,
                },

                Employee = new Models.Employee
                {
                    Id = order.Employee.Id,
                    FirstName = order.Employee.FirstName,
                    LastName = order.Employee.LastName,
                    Country = order.Employee.Country,
                },

                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,

                Shipper = new Models.Shipper
                {
                    Id = order.Shipper.Id,
                    CompanyName = order.Shipper.CompanyName,
                },

                Freight = order.Freight,
                ShipName = order.ShipName,

                ShippingAddress = new Models.ShippingAddress
                {
                    Address = order.ShippingAddress.Address,
                    City = order.ShippingAddress.City,
                    Region = order.ShippingAddress.Region,
                    PostalCode = order.ShippingAddress.PostalCode,
                    Country = order.ShippingAddress.Country,
                },

                OrderDetails = order.OrderDetails
                    .Select(detail => new FullOrderDetail
                    {
                        ProductId = detail.Product.Id,
                        ProductName = detail.Product.ProductName,

                        CategoryId = detail.Product.CategoryId,
                        CategoryName = detail.Product.Category,

                        SupplierId = detail.Product.SupplierId,
                        SupplierCompanyName = detail.Product.Supplier,

                        UnitPrice = detail.UnitPrice,
                        Quantity = detail.Quantity,
                        Discount = detail.Discount,
                    })
                    .ToList(),
            };

            return this.Ok(result);
        }
        catch (OrderNotFoundException exception)
        {
            this.logger.LogError(
                exception,
                "Order with ID {OrderId} was not found.",
                orderId);

            return this.NotFound();
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "An error occurred while getting order with ID {OrderId}.",
                orderId);

            return this.StatusCode(
                StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet]
    public Task<ActionResult<IEnumerable<BriefOrder>>> GetOrdersAsync([FromQuery] int? skip = 0, [FromQuery] int? count = 10)
    {
        int actualSkip = skip ?? 0;
        int actualCount = count ?? 10;

        if (actualSkip < 0 || actualCount <= 0)
        {
            this.logger.LogError("Invalid pagination. Skip: {Skip}, Count: {Count}.", actualSkip, actualCount);
            return Task.FromResult<ActionResult<IEnumerable<BriefOrder>>>(this.BadRequest());
        }

        return this.GetOrdersAsyncCore(actualSkip, actualCount);
    }


    [HttpPost]
    public Task<ActionResult<AddOrder>> AddOrderAsync([FromBody] BriefOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return this.AddOrderAsyncCore(order);
    }


    [HttpDelete("{orderId:long}")]
    public async Task<ActionResult> RemoveOrderAsync(long orderId)
    {
        try
        {
            this.logger.LogTrace(
                "Removing order with ID {OrderId}.",
                orderId);

            await this.repository.RemoveOrderAsync(orderId);

            return this.NoContent();
        }
        catch (OrderNotFoundException exception)
        {
            this.logger.LogError(
                exception,
                "Order with ID {OrderId} was not found.",
                orderId);

            return this.NotFound();
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "An error occurred while removing order with ID {OrderId}.",
                orderId);

            return this.StatusCode(
                StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{orderId:long}")]
    public Task<ActionResult> UpdateOrderAsync(long orderId, [FromBody] BriefOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return this.UpdateOrderAsyncCore(orderId, order);
    }


    private static Order MapBriefOrder(BriefOrder source, long? orderId = null)
    {
        var order = new Order(
            orderId ?? source.Id)
        {
            Customer = new Northwind.Services.Repositories.Customer(
                new CustomerCode(source.CustomerId))
            {
                CompanyName = string.Empty,
            },

            Employee = new Northwind.Services.Repositories.Employee(
                source.EmployeeId)
            {
                FirstName = string.Empty,
                LastName = string.Empty,
                Country = string.Empty,
            },

            OrderDate = source.OrderDate,
            RequiredDate = source.RequiredDate,
            ShippedDate = source.ShippedDate,

            Shipper = new Northwind.Services.Repositories.Shipper(
                source.ShipperId)
            {
                CompanyName = string.Empty,
            },

            Freight = source.Freight,
            ShipName = source.ShipName,

            ShippingAddress =
                new Northwind.Services.Repositories.ShippingAddress(
                    source.ShipAddress,
                    source.ShipCity,
                    source.ShipRegion,
                    source.ShipPostalCode,
                    source.ShipCountry),
        };

        foreach (var sourceDetail in source.OrderDetails)
        {
            var detail =
                new OrderDetail(order)
                {
                    Product =
                        new Product(
                            sourceDetail.ProductId),

                    UnitPrice = sourceDetail.UnitPrice,
                    Quantity = sourceDetail.Quantity,
                    Discount = sourceDetail.Discount,
                };

            order.OrderDetails.Add(detail);
        }

        return order;
    }

    private async Task<ActionResult<IEnumerable<BriefOrder>>> GetOrdersAsyncCore(int actualSkip, int actualCount)
    {
        try
        {
            this.logger.LogTrace(
                "Getting orders. Skip: {Skip}, Count: {Count}.",
                actualSkip,
                actualCount);

            var orders = await this.repository.GetOrdersAsync(
                actualSkip,
                actualCount);

            var result = orders.Select(order => new BriefOrder
            {
                Id = order.Id,
                CustomerId = order.Customer.Code.Code,
                EmployeeId = order.Employee.Id,
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,
                ShipperId = order.Shipper.Id,
                Freight = order.Freight,
                ShipName = order.ShipName,
                ShipAddress = order.ShippingAddress.Address,
                ShipCity = order.ShippingAddress.City,
                ShipRegion = order.ShippingAddress.Region,
                ShipPostalCode = order.ShippingAddress.PostalCode,
                ShipCountry = order.ShippingAddress.Country,

                OrderDetails = order.OrderDetails
                    .Select(detail => new BriefOrderDetail
                    {
                        ProductId = detail.Product.Id,
                        UnitPrice = detail.UnitPrice,
                        Quantity = detail.Quantity,
                        Discount = detail.Discount,
                    })
                    .ToList(),
            }).ToList();

            return this.Ok(result);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "An error occurred while getting orders.");

            return this.StatusCode(
                StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<ActionResult<AddOrder>> AddOrderAsyncCore(BriefOrder order)
    {
        try
        {
            this.logger.LogTrace("Adding a new order.");

            var repositoryOrder = MapBriefOrder(order);

            long orderId =
                await this.repository.AddOrderAsync(repositoryOrder);

            return this.Ok(new AddOrder
            {
                OrderId = orderId,
            });
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "An error occurred while adding an order.");

            return this.StatusCode(
                StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<ActionResult> UpdateOrderAsyncCore(long orderId, BriefOrder order)
    {
        try
        {
            this.logger.LogTrace(
                "Updating order with ID {OrderId}.",
                orderId);

            var repositoryOrder = MapBriefOrder(order, orderId);

            await this.repository.UpdateOrderAsync(repositoryOrder);

            return this.NoContent();
        }
        catch (OrderNotFoundException exception)
        {
            this.logger.LogError(
                exception,
                "Order with ID {OrderId} was not found.",
                orderId);

            return this.NotFound();
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "An error occurred while updating order with ID {OrderId}.",
                orderId);

            return this.StatusCode(
                StatusCodes.Status500InternalServerError);
        }
    }
}
