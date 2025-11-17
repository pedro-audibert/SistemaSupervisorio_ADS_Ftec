using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    // Modelo de exibição para a Análise de OEE (UC6)
    public class AnaliseOEEViewModel
    {
        // --- 1. MÉTRICAS PRINCIPAIS (KPIs) ---
        [Display(Name = "OEE Global")]
        public decimal OEE { get; set; }           // Eficiência Global (0 a 100)

        [Display(Name = "Disponibilidade")]
        public decimal Disponibilidade { get; set; } // Disponibilidade (0 a 100)

        [Display(Name = "Performance")]
        public decimal Performance { get; set; }     // Performance (0 a 100)

        [Display(Name = "Qualidade")]
        public decimal Qualidade { get; set; }       // Qualidade (0 a 100)

        // --- 2. CONTEXTO DO PERÍODO ---
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string MaquinaId { get; set; }
        public string NomeMaquina { get; set; }

        // --- 3. ANÁLISE DE TEMPO ---
        public TimeSpan TempoCarga { get; set; }
        public TimeSpan TempoOperacionalPlanejado { get; set; }
        public TimeSpan TempoParadasNaoPlanejadas { get; set; }
        public TimeSpan TempoParadasAguardando { get; set; }

        // --- 4. ANÁLISE DE PRODUÇÃO ---
        public long ContagemTotal { get; set; }
        public long ContagemIdeal { get; set; }
        public long RefugoQualidade { get; set; }

        // --- 5. CONFIGURAÇÃO (UC7) ---
        // Flag para Lançamento Manual de Refugo
        public bool HabilitarRefugoManual { get; set; }
        public int VelocidadeIdealPorHora { get; set; }
        public int TaxaAtualizacaoMinutos { get; set; }
        public TimeSpan TempoOperacionalLiquido { get; set; }

        // --- 6. DETALHE DE PERDAS ---
        public List<PerdaDetalheViewModel> PerdasNaoPlanejadas { get; set; } = new List<PerdaDetalheViewModel>();
    }

    public class PerdaDetalheViewModel
    {
        public string CodigoEvento { get; set; }
        public string Descricao { get; set; }
        public TimeSpan TempoTotalParado { get; set; }
        public int Frequencia { get; set; }
    }
}