using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;

namespace Grand.Business.Marketing.Services.Customers
{
    /// <summary>
    /// Customer tag service
    /// </summary>
    public partial class CustomerTagService : ICustomerTagService
    {
        #region Fields

        private readonly IRepository<CustomerTag> _customerTagRepository;
        private readonly IRepository<CustomerTagProduct> _customerTagProductRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public CustomerTagService(IRepository<CustomerTag> customerTagRepository,
            IRepository<CustomerTagProduct> customerTagProductRepository,
            IRepository<Customer> customerRepository,
            IMediator mediator,
            ICacheBase cacheBase
            )
        {
            _customerTagRepository = customerTagRepository;
            _customerTagProductRepository = customerTagProductRepository;
            _mediator = mediator;
            _customerRepository = customerRepository;
            _cacheBase = cacheBase;
        }

        #endregion

        /// <summary>
        /// Gets all customer for tag id
        /// </summary>
        /// <returns>Customers</returns>
        public virtual async Task<IPagedList<Customer>> GetCustomersByTag(string customerTagId = "", int pageIndex = 0, int pageSize = 2147483647)
        {
            var query = from c in _customerRepository.Table
                        where c.CustomerTags.Contains(customerTagId)
                        select c;
            return await PagedList<Customer>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Delete a customer tag
        /// </summary>
        /// <param name="customerTag">Customer tag</param>
        public virtual async Task DeleteCustomerTag(CustomerTag customerTag)
        {
            if (customerTag == null)
                throw new ArgumentNullException(nameof(customerTag));

            //update customer
            await _customerRepository.Pull(string.Empty, x => x.CustomerTags, customerTag.Id, true);

            //delete
            await _customerTagRepository.DeleteAsync(customerTag);

            //event notification
            await _mediator.EntityDeleted(customerTag);
        }

        /// <summary>
        /// Gets all customer tags
        /// </summary>
        /// <returns>Customer tags</returns>
        public virtual async Task<IList<CustomerTag>> GetAllCustomerTags()
        {
            return await Task.FromResult(_customerTagRepository.Table.ToList());
        }

        /// <summary>
        /// Gets customer tag
        /// </summary>
        /// <param name="customerTagId">Customer tag identifier</param>
        /// <returns>Customer tag</returns>
        public virtual Task<CustomerTag> GetCustomerTagById(string customerTagId)
        {
            return _customerTagRepository.GetByIdAsync(customerTagId);
        }

        /// <summary>
        /// Gets customer tag by name
        /// </summary>
        /// <param name="name">Customer tag name</param>
        /// <returns>Customer tag</returns>
        public virtual async Task<CustomerTag> GetCustomerTagByName(string name)
        {
            var query = from pt in _customerTagRepository.Table
                        where pt.Name == name
                        select pt;

            return await Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Gets customer tags search by name
        /// </summary>
        /// <param name="name">Customer tags name</param>
        /// <returns>Customer tags</returns>
        public virtual async Task<IList<CustomerTag>> GetCustomerTagsByName(string name)
        {
            var query = from pt in _customerTagRepository.Table
                        where pt.Name.ToLower().Contains(name.ToLower())
                        select pt;
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Inserts a customer tag
        /// </summary>
        /// <param name="customerTag">Customer tag</param>
        public virtual async Task InsertCustomerTag(CustomerTag customerTag)
        {
            if (customerTag == null)
                throw new ArgumentNullException(nameof(customerTag));

            await _customerTagRepository.InsertAsync(customerTag);

            //event notification
            await _mediator.EntityInserted(customerTag);
        }

        /// <summary>
        /// Insert tag to a customer
        /// </summary>
        public virtual async Task InsertTagToCustomer(string customerTagId, string customerId)
        {
            await _customerRepository.AddToSet(customerId, x => x.CustomerTags, customerTagId);
        }

        /// <summary>
        /// Delete tag from a customer
        /// </summary>
        public virtual async Task DeleteTagFromCustomer(string customerTagId, string customerId)
        {
            await _customerRepository.Pull(customerId, x => x.CustomerTags, customerTagId);
        }

        /// <summary>
        /// Updates the customer tag
        /// </summary>
        /// <param name="customerTag">Customer tag</param>
        public virtual async Task UpdateCustomerTag(CustomerTag customerTag)
        {
            if (customerTag == null)
                throw new ArgumentNullException(nameof(customerTag));

            await _customerTagRepository.UpdateAsync(customerTag);

            //event notification
            await _mediator.EntityUpdated(customerTag);
        }

        /// <summary>
        /// Get number of customers
        /// </summary>
        /// <param name="customerTagId">Customer tag identifier</param>
        /// <returns>Number of customers</returns>
        public virtual async Task<int> GetCustomerCount(string customerTagId)
        {
            var query = _customerRepository.Table.
                Where(x => x.CustomerTags.Contains(customerTagId)).
                GroupBy(p => p, (k, s) => new { Counter = s.Count() }).ToList();
            if (query.Count > 0)
                return query.FirstOrDefault().Counter;
            return await Task.FromResult(0);
        }

        #region Customer tag product


        /// <summary>
        /// Gets customer tag products for customer tag
        /// </summary>
        /// <param name="customerTagId">Customer tag id</param>
        /// <returns>Customer tag products</returns>
        public virtual async Task<IList<CustomerTagProduct>> GetCustomerTagProducts(string customerTagId)
        {
            string key = string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, customerTagId);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from cr in _customerTagProductRepository.Table
                            where (cr.CustomerTagId == customerTagId)
                            orderby cr.DisplayOrder
                            select cr;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets customer tag products for customer tag
        /// </summary>
        /// <param name="customerTagId">Customer tag id</param>
        /// <param name="productId">Product id</param>
        /// <returns>Customer tag product</returns>
        public virtual async Task<CustomerTagProduct> GetCustomerTagProduct(string customerTagId, string productId)
        {
            var query = from cr in _customerTagProductRepository.Table
                        where cr.CustomerTagId == customerTagId && cr.ProductId == productId
                        orderby cr.DisplayOrder
                        select cr;
            return await Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Gets customer tag product
        /// </summary>
        /// <param name="Id">id</param>
        /// <returns>Customer tag product</returns>
        public virtual Task<CustomerTagProduct> GetCustomerTagProductById(string id)
        {
            return _customerTagProductRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Inserts a customer tag product
        /// </summary>
        /// <param name="customerTagProduct">Customer tag product</param>
        public virtual async Task InsertCustomerTagProduct(CustomerTagProduct customerTagProduct)
        {
            if (customerTagProduct == null)
                throw new ArgumentNullException(nameof(customerTagProduct));

            await _customerTagProductRepository.InsertAsync(customerTagProduct);

            //clear cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, customerTagProduct.CustomerTagId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);

            //event notification
            await _mediator.EntityInserted(customerTagProduct);
        }

        /// <summary>
        /// Updates the customer tag product
        /// </summary>
        /// <param name="customerTagProduct">Customer tag product</param>
        public virtual async Task UpdateCustomerTagProduct(CustomerTagProduct customerTagProduct)
        {
            if (customerTagProduct == null)
                throw new ArgumentNullException(nameof(customerTagProduct));

            await _customerTagProductRepository.UpdateAsync(customerTagProduct);

            //clear cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, customerTagProduct.CustomerTagId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);

            //event notification
            await _mediator.EntityUpdated(customerTagProduct);
        }

        /// <summary>
        /// Delete a customer tag product
        /// </summary>
        /// <param name="customerTagProduct">Customer tag product</param>
        public virtual async Task DeleteCustomerTagProduct(CustomerTagProduct customerTagProduct)
        {
            if (customerTagProduct == null)
                throw new ArgumentNullException(nameof(customerTagProduct));

            await _customerTagProductRepository.DeleteAsync(customerTagProduct);

            //clear cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.CUSTOMERTAGPRODUCTS_ROLE_KEY, customerTagProduct.CustomerTagId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);
            //event notification
            await _mediator.EntityDeleted(customerTagProduct);
        }

        #endregion

    }
}
