using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using p48_lessons_asp.Data;
using p48_lessons_asp.Data.Entity;
using p48_lessons_asp.DTO.Product;
using p48_lessons_asp.Models;
using p48_lessons_asp.Models.ProductRequestModels;
using p48_lessons_asp.Services.FluentValidation;
using p48_lessons_asp.Services.Slug;

namespace p48_lessons_asp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly DataContext _dataContext;
        private readonly IValidator<CreateProductModel> _validator;
        private readonly ISlugService _slugService;
        private readonly IMapper _mapper;

        public ProductsController(
            DataContext dataContext, 
            IValidator<CreateProductModel> validator, 
            ISlugService slugService, 
            IMapper mapper)
        {
            _dataContext = dataContext;
            _validator = validator;
            _slugService = slugService;
            _mapper = mapper;
        }

        // Отримання списку всіх продуктів
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _dataContext.Products.ToListAsync();
                
                if (products == null || products.Count == 0)
                {
                    return NotFound(new ResponseModel<object>
                    {
                        StatusCode = 404,
                        Message = "Products not found"
                    });
                }

                return Ok(new ResponseModel<List<Product>>
                {
                    StatusCode = 200,
                    Message = "Ok",
                    Data = products
                });
            }
            catch (OperationCanceledException ex)
            {
                System.Console.WriteLine($"[CRITICAL] Get products error: {ex.Message} | {DateTime.Now}");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel<object>
                {
                    StatusCode = 500,
                    Message = "Cannot retrieve products. See server logs"
                });
            }
        }

        // Створення продукту з валідацією та мапінгом
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductModel model)
        {
            // 1. Валідація моделі через FluentValidation
            var validationResult = await _validator.GetErrorsAsync(model);
            if (validationResult != null)
            {
                return BadRequest(validationResult);
            }

            // 2. Створення сутності
            var product = new Product
            {
                Name = model.Name,
                Slug = _slugService.GetSlug(model.Name),
                CategoryId = model.CategoryId,
                Price = model.Price,
                Description = model.Description
            };

            try
            {
                // 3. Збереження в базу даних
                await _dataContext.Products.AddAsync(product);
                await _dataContext.SaveChangesAsync();

                // 4. Повернення успішної відповіді (201 Created)
                return StatusCode(201, new
                {
                    StatusCode = 201,
                    Message = "Product created successfully",
                    Data = _mapper.Map<ProductDto>(product)
                });
            }
            catch (OperationCanceledException ex)
            {
                System.Console.WriteLine($"[CRITICAL] Create product error: {ex.Message} | {DateTime.Now}");
                
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    StatusCode = 503,
                    Message = $"Service is unavailable: {ex.Message}",
                    Data = (object)null!
                });
            }
        }
    }
}