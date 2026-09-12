using Microsoft.EntityFrameworkCore;
using Northwind.Services.EntityFramework.Entities;
using Northwind.Services.Repositories;
using EntityOrder = Northwind.Services.EntityFramework.Entities.Order;
using EntityOrderDetail = Northwind.Services.EntityFramework.Entities.OrderDetail;
using RepositoryCustomer = Northwind.Services.Repositories.Customer;
using RepositoryEmployee = Northwind.Services.Repositories.Employee;
using RepositoryOrder = Northwind.Services.Repositories.Order;
using RepositoryOrderDetail = Northwind.Services.Repositories.OrderDetail;
using RepositoryProduct = Northwind.Services.Repositories.Product;
using RepositoryShipper = Northwind.Services.Repositories.Shipper;

namespace Northwind.Services.EntityFramework.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly NorthwindContext context;

    public OrderRepository(NorthwindContext context)
    {
        this.context = context;
    }

    public async Task<RepositoryOrder> GetOrderAsync(long orderId)
    {
        var entity = await this.GetOrdersQuery()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (entity is null)
        {
            throw new OrderNotFoundException();
        }

        return MapOrder(entity);
    }

    public Task<IList<RepositoryOrder>> GetOrdersAsync(int skip, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        return this.GetOrdersAsyncCore(skip, count);
    }

    public Task<long> AddOrderAsync(RepositoryOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return this.AddOrderAsyncCore(order);
    }

    public async Task RemoveOrderAsync(long orderId)
    {
        var entity = await this.context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (entity is null)
        {
            throw new OrderNotFoundException();
        }

        this.context.OrderDetails.RemoveRange(entity.OrderDetails);
        this.context.Orders.Remove(entity);

        await this.context.SaveChangesAsync();
    }

    public Task UpdateOrderAsync(RepositoryOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return this.UpdateOrderAsyncCore(order);
    }

    private static RepositoryOrder MapOrder(EntityOrder entity)
    {
        var customer = entity.Customer
            ?? throw new InvalidOperationException(
                $"Order {entity.OrderId} has no customer.");

        var employee = entity.Employee
            ?? throw new InvalidOperationException(
                $"Order {entity.OrderId} has no employee.");

        var shipper = entity.Shipper
            ?? throw new InvalidOperationException(
                $"Order {entity.OrderId} has no shipper.");

        if (entity.OrderDate is null)
        {
            throw new InvalidOperationException(
                $"Order {entity.OrderId} has no order date.");
        }

        if (entity.RequiredDate is null)
        {
            throw new InvalidOperationException(
                $"Order {entity.OrderId} has no required date.");
        }

        var order = new RepositoryOrder(entity.OrderId)
        {
            Customer = new RepositoryCustomer(
                new CustomerCode(customer.Id))
            {
                CompanyName = customer.CompanyName,
            },

            Employee = new RepositoryEmployee(employee.EmployeeId)
            {
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Country = employee.Country ?? string.Empty,
            },

            OrderDate = entity.OrderDate.Value,
            RequiredDate = entity.RequiredDate.Value,
            ShippedDate = entity.ShippedDate,

            Shipper = new RepositoryShipper(shipper.ShipperId)
            {
                CompanyName = shipper.CompanyName,
            },

            Freight = (double)(entity.Freight ?? 0m),

            ShipName = entity.ShipName ?? string.Empty,

            ShippingAddress = new ShippingAddress(
                entity.ShipAddress ?? string.Empty,
                entity.ShipCity ?? string.Empty,
                entity.ShipRegion,
                entity.ShipPostalCode ?? string.Empty,
                entity.ShipCountry ?? string.Empty),
        };

        foreach (var detailEntity in entity.OrderDetails)
        {
            var productEntity = detailEntity.Product;

            var detail = new RepositoryOrderDetail(order)
            {
                Product = new RepositoryProduct(productEntity.ProductId)
                {
                    ProductName = productEntity.ProductName,
                    SupplierId = productEntity.SupplierId ?? 0,
                    Supplier = productEntity.Supplier?.CompanyName
                        ?? string.Empty,
                    CategoryId = productEntity.CategoryId ?? 0,
                    Category = productEntity.Category?.CategoryName
                        ?? string.Empty,
                },

                UnitPrice = (double)detailEntity.UnitPrice,
                Quantity = detailEntity.Quantity,
                Discount = detailEntity.Discount,
            };

            order.OrderDetails.Add(detail);
        }

        return order;
    }

    private async Task<IList<RepositoryOrder>> GetOrdersAsyncCore(
        int skip,
        int count)
    {
        var entities = await this.GetOrdersQuery()
            .OrderBy(o => o.OrderId)
            .Skip(skip)
            .Take(count)
            .ToListAsync();

        return entities
            .Select(MapOrder)
            .ToList();
    }

    private async Task<long> AddOrderAsyncCore(RepositoryOrder order)
    {
        try
        {
            var entity = new EntityOrder
            {
                CustomerId = order.Customer.Code.Code,
                EmployeeId = checked((int)order.Employee.Id),
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,
                ShipVia = checked((int)order.Shipper.Id),
                Freight = (decimal)order.Freight,
                ShipName = order.ShipName,
                ShipAddress = order.ShippingAddress.Address,
                ShipCity = order.ShippingAddress.City,
                ShipRegion = order.ShippingAddress.Region,
                ShipPostalCode = order.ShippingAddress.PostalCode,
                ShipCountry = order.ShippingAddress.Country,
            };

            foreach (var detail in order.OrderDetails)
            {
                entity.OrderDetails.Add(
                    new EntityOrderDetail
                    {
                        ProductId = detail.Product.Id,
                        UnitPrice = (decimal)detail.UnitPrice,
                        Quantity = detail.Quantity,
                        Discount = detail.Discount,
                    });
            }

            this.context.Orders.Add(entity);

            await this.context.SaveChangesAsync();

            return entity.OrderId;
        }
        catch (Exception exception) when (
            exception is DbUpdateException or OverflowException or InvalidOperationException)
        {
            throw new RepositoryException(
                "An error occurred while adding an order.",
                exception);
        }
    }

    private async Task UpdateOrderAsyncCore(RepositoryOrder order)
    {
        var entity = await this.context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == order.Id);

        if (entity is null)
        {
            throw new OrderNotFoundException();
        }

        entity.CustomerId = order.Customer.Code.Code;
        entity.EmployeeId = checked((int)order.Employee.Id);
        entity.OrderDate = order.OrderDate;
        entity.RequiredDate = order.RequiredDate;
        entity.ShippedDate = order.ShippedDate;
        entity.ShipVia = checked((int)order.Shipper.Id);
        entity.Freight = (decimal)order.Freight;
        entity.ShipName = order.ShipName;
        entity.ShipAddress = order.ShippingAddress.Address;
        entity.ShipCity = order.ShippingAddress.City;
        entity.ShipRegion = order.ShippingAddress.Region;
        entity.ShipPostalCode = order.ShippingAddress.PostalCode;
        entity.ShipCountry = order.ShippingAddress.Country;

        this.context.OrderDetails.RemoveRange(entity.OrderDetails);
        entity.OrderDetails.Clear();

        foreach (var detail in order.OrderDetails)
        {
            entity.OrderDetails.Add(
                new EntityOrderDetail
                {
                    OrderId = entity.OrderId,
                    ProductId = detail.Product.Id,
                    UnitPrice = (decimal)detail.UnitPrice,
                    Quantity = detail.Quantity,
                    Discount = detail.Discount,
                });
        }

        await this.context.SaveChangesAsync();
    }

    private IQueryable<EntityOrder> GetOrdersQuery()
    {
        return this.context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.Shipper)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.Supplier)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.Category);
    }
}
