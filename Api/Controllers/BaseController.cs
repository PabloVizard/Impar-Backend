﻿using Application.Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController<Entity, Model> : ControllerBase
        where Entity : BaseEntity
        where Model : BaseModel
    {
        protected readonly IBaseApp<Entity, Model> _baseApp;

        public BaseController(IBaseApp<Entity, Model> baseApp)
        {
            _baseApp = baseApp;
        }

        [HttpGet]
        [Route("ObterPorId")]
        public virtual async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                return Ok(await _baseApp.FindAsync(id));
            }
            catch (Exception er)
            {
                return BadRequest("Erro Inesperado: " + er.Message);
            }
        }

        [HttpGet]
        [Route("ObterTodosPaginados")]
        public virtual async Task<IActionResult> ObterTodosPaginados(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var retorno = await _baseApp.ListPagedAsync(pageNumber, pageSize);

                return Ok(retorno);
            }
            catch (Exception er)
            {
                return BadRequest("Erro Inesperado: " + er.Message);
            }
        }

        [HttpPut]
        [Route("Atualizar")]
        public virtual async Task<IActionResult> Atualizar(Model dados)
        {
            try
            {
                var dadosFind = await _baseApp.FindAsync(dados.Id);
                if (dadosFind == null)
                {
                    return BadRequest("Dados não existentes");
                }

                var dadosProperties = dados.GetType().GetProperties();
                var dadosFindProperties = dadosFind.GetType().GetProperties();

                var propertyValueMap = new Dictionary<string, object>();
                foreach (var property in dadosProperties)
                {
                    propertyValueMap[property.Name] = property.GetValue(dados);
                }

                foreach (var property in dadosFindProperties)
                {
                    if (propertyValueMap.ContainsKey(property.Name))
                    {
                        property.SetValue(dadosFind, propertyValueMap[property.Name]);
                    }
                }

                _baseApp.Update(dadosFind);
                await _baseApp.SaveChangesAsync();

                return Ok(dadosFind);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlException && sqlException.Message.Contains("Violation of UNIQUE KEY constraint"))
                {
                    return BadRequest("O registro com o mesmo valor já existe. Por favor, verifique seus dados.");
                }

                return BadRequest("Erro Inesperado");
            }
        }

        [HttpPost]
        [Route("Registrar")]
        public virtual async Task<IActionResult> Registrar(Model dados)
        {
            try
            {
                var data = await _baseApp.Add(dados);
                await _baseApp.SaveChangesAsync();
                var dataEntity = (EntityEntry<Entity>)data;
                return Ok(dataEntity.Entity);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlException && sqlException.Message.Contains("Violation of UNIQUE KEY constraint"))
                {
                    return BadRequest("O registro com o mesmo valor já existe. Por favor, verifique seus dados.");
                }

                return BadRequest("Erro Inesperado");
            }
        }

        [HttpDelete]
        [Route("Remover")]
        public virtual async Task<IActionResult> Remover(int id)
        {
            try
            {
                var dados = await _baseApp.FindAsync(id);
                if (dados == null)
                {
                    return BadRequest("Dados não encontrados.");
                }

                _baseApp.Remove(dados);
                await _baseApp.SaveChangesAsync();
                return Ok(new
                {
                    data = dados,
                    message = "Removido com sucesso."
                });
            }
            catch (Exception ex)
            {
                return BadRequest("Erro Inesperado: " + ex.Message);
            }
        }

    }
}
