using mmdba.Models;
using System;
using System.Threading.Tasks;

namespace mmdba.Services
{
    public interface IOeeService
    {
        /// <summary>
        /// Calcula os KPIs de OEE (Disponibilidade, Performance, Qualidade) para um determinado período.
        /// </summary>
        /// <param name="maquinaId">O identificador da máquina.</param>
        /// <param name="dataInicio">Início do período de análise.</param>
        /// <param name="dataFim">Fim do período de análise.</param>
        /// <param name="turnoId">Opcional. ID do turno para análise (0 ou null para todos).</param>
        /// <returns>Um modelo de visualização com todos os dados de OEE calculados.</returns>
        Task<AnaliseOEEViewModel> CalcularOEEAsync(
            string maquinaId,
            DateTime dataInicio,
            DateTime dataFim,
            int? turnoId);
    }
}