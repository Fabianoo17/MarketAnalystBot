fetch('/tickerboard/candles?ticker=PETR4')
    .then(r => r.json())
    .then(data => {

        const ohlc = data.map(x => [
            x.time,
            x.open,
            x.high,
            x.low,
            x.close
        ]);

        Highcharts.stockChart('container', {
            rangeSelector: {
                selected: 2
            },

            title: {
                text: 'PETR4 - Diário'
            },

            yAxis: {
                labels: {
                    align: 'right',
                    x: -3
                },
                title: {
                    text: 'Preço'
                },
                height: '100%',
                lineWidth: 2
            },

            series: [{
                type: 'candlestick',
                name: 'PETR4',
                data: ohlc,
                tooltip: {
                    valueDecimals: 2
                }
            }]
        });
    });