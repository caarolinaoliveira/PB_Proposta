using PB.Proposta.Application.Services;
using PB.Proposta.Application.Response;
using PB.Proposta.Application.Events;
using PB.Proposta.Application.Interfaces;
using System.Net;
using Microsoft.AspNetCore.Mvc;


namespace PB.Proposta.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/propostas")]
    public class PropostaController : ControllerBase
    {
        private readonly IPropostaService _propostaService;

        public PropostaController(IPropostaService propostaService)
        {
            _propostaService = propostaService;
            
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PropostaResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ObterPropostaPorIdCliente (Guid id)
        {
            var proposta = await _propostaService.ObterPropostaPorIdCliente(id);

            if (proposta == null)
                return NotFound();

            return Ok(proposta);
        }

    }
    
}