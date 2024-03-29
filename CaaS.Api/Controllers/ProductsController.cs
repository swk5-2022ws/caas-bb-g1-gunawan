﻿using AutoMapper;
using CaaS.DTO;
using Microsoft.AspNetCore.Mvc;
//using CaaS.Api.BackgroundServices;
using Dal.Common;
using CaaS.Logic;
using CaaS.Domain;
using Microsoft.AspNetCore.Authorization;

namespace CaaS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiConventionType(typeof(WebApiConventions))]
    public class ProductsController : ControllerBase
    {
        private readonly IManagementLogic<Product> logic;
        private readonly IMapper mapper;
        //private readonly UpdateChannel updateChannel;

        //public ProductsController( IMapper mapper, UpdateChannel updateChannel, string table)
        public ProductsController(IManagementLogic<Product> logic, IMapper mapper)
        {           
            this.logic = logic ?? throw new ArgumentNullException(nameof(logic));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            //this.updateChannel = updateChannel ?? throw new ArgumentNullException(nameof(updateChannel));
        }
                
        
        private async Task<ProductDTO?> GetLast()
        {
            var products = await logic.Get();          
            return mapper.Map<ProductDTO>(products.LastOrDefault());
        } 
       
        
        /// <summary>
        /// Returns a Product by name or description.
        /// </summary>
        /// <param name="Name">Name</param>
        /// <param name="Description">Description</param>
        /// <returns>The Product with the given name or description</returns>
        [HttpGet("{Name}/{Description}")]
        public async Task<IEnumerable<ProductDTO>> GetProductByName( String Name,  String Description)
        {
            var products = await logic.Get();
            List<string> txtList = Name.Split(' ').ToList();
            List<string> txtList2 = Description.Split(' ').ToList();

            return mapper.Map<IEnumerable<ProductDTO>>( products.Where(
                p => txtList.Any(word=>p.Name.Contains(word) 
                || txtList2.Any(word => p.ProductDesc.Contains(word))
                )
              )
            ) ??  Enumerable.Empty<ProductDTO>();
        }

        /// <summary>
        /// Returns a Product by ProductID.
        /// </summary>
        /// <param name="productId">ID</param>      
        /// <returns>The Product with the given ID</returns>
        [HttpGet("{productId}")]
        public async Task<ActionResult<ProductDTO>> GetProductById(String productId)
        {
            Product? prod = await logic.Search(productId);
            if (prod is null)
            {
                return NotFound(StatusInfo.InvalidProductId(productId));
            }
            //return Ok(Product.ToDto());
            return mapper.Map<ProductDTO>(prod);
        }

        /// <summary>
        /// Returns a list of products
        /// </summary>
        /// <returns>a list of products</returns>
        [HttpGet]
        public async Task<IEnumerable<ProductDTO>> GetProducts()
        {
            var products = await logic.Get();
            return mapper.Map<IEnumerable<ProductDTO>>(products);
        }

        /// <summary>
        /// Returns a created product with the id after creating a product
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ProductDTO>> CreateProduct([FromBody] ProductForCreationDTO productDTO)
        {         
            Domain.Product product = mapper.Map<Domain.Product>(productDTO);
            var productDTO2 = await logic.Get();
       
            product.Id = Util.createID(productDTO2.LastOrDefault().Id,productDTO2.Count());
            await logic.Add(product);
            return CreatedAtAction(actionName: nameof(GetProductById),
                routeValues: new { ProductId = product.Id },
                //value: Product.ToDto()
                value: mapper.Map<ProductDTO>(product)
                );
        }

        /// <summary>
        /// Update a product with the product id given and show it back if success updating
        /// </summary>
        [HttpPut("{productId}")]
        [Authorize]
        public async Task<ActionResult<ProductDTO>> UpdateProduct(String productId,[FromBody] ProductForCreationDTO productDTO)
        {
            Domain.Product? product = (Product?) await logic.Search(productId);
            if (product is null) {
                return NotFound();
            }

            mapper.Map(productDTO,product);

            await logic.Update(product);
            return NoContent();
        }


        /// <summary>
        /// Delete a product with the product-id given
        /// </summary>
        [HttpDelete("{productId}")]
        [Authorize]
        public async Task<ActionResult> DeleteProduct([FromRoute] String productId)
        {
            if (await logic.Delete(productId))
            {
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }
       
    }
}
