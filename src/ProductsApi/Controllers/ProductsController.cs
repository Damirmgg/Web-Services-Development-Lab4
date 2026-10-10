using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ProductsApi.DTOs;
using ProductsApi.Models;
using ProductsApi.Repositories;

namespace ProductsApi.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public ProductsController(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    /// <summary>Получить все товары.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ReturnResult<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReturnResult<IEnumerable<ProductDto>>>> GetAll()
    {
        var products = await _repository.GetAllAsync();
        var dto = _mapper.Map<IEnumerable<ProductDto>>(products);
        return Ok(ReturnResult<IEnumerable<ProductDto>>.Success(dto));
    }

    /// <summary>Получить товар по идентификатору.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReturnResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReturnResult<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnResult<ProductDto>>> GetById(int id)
    {
        var product = await _repository.GetAsync(id);
        if (product is null)
            return NotFound(ReturnResult<ProductDto>.Failure($"Товар с ID {id} не найден."));

        return Ok(ReturnResult<ProductDto>.Success(_mapper.Map<ProductDto>(product)));
    }

    /// <summary>Создать товар. Category сохраняется в базе, но не возвращается в ответе.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReturnResult<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReturnResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReturnResult<ProductDto>>> Create(ProductRequestDto request)
    {
        var product = _mapper.Map<Product>(request);
        await _repository.CreateAsync(product);

        var dto = _mapper.Map<ProductDto>(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ReturnResult<ProductDto>.Success(dto));
    }

    /// <summary>Заменить название, цену и категорию товара.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReturnResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReturnResult<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ReturnResult<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnResult<ProductDto>>> Update(int id, ProductRequestDto request)
    {
        var product = await _repository.GetAsync(id);
        if (product is null)
            return NotFound(ReturnResult<ProductDto>.Failure($"Товар с ID {id} не найден."));

        _mapper.Map(request, product);
        await _repository.UpdateAsync(product);

        return Ok(ReturnResult<ProductDto>.Success(_mapper.Map<ProductDto>(product)));
    }

    /// <summary>Удалить товар.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ReturnResult<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReturnResult<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReturnResult<bool>>> Delete(int id)
    {
        if (await _repository.GetAsync(id) is null)
            return NotFound(ReturnResult<ProductDto>.Failure($"Товар с ID {id} не найден."));

        await _repository.DeleteAsync(id);
        return Ok(ReturnResult<bool>.Success(true));
    }
}
