// =============================================================
// ADMIN DASHBOARD CHARTS
// =============================================================


// =============================================================
// DOUGHNUT CENTER TEXT PLUGIN
// =============================================================

const doughnutCenterText = {

    id: "doughnutCenterText",

    afterDraw(chart) {

        const { ctx } = chart;

        const dataset =
            chart.data.datasets[0];

        if (
            !dataset ||
            !dataset.data ||
            !chart.chartArea
        ) {
            return;
        }


        // =====================================================
        // TOTAL
        // =====================================================

        const total =
            dataset.data.reduce(
                (sum, value) =>
                    sum + Number(value),
                0
            );


        // =====================================================
        // CENTER POSITION
        // =====================================================

        const centerX =
            (
                chart.chartArea.left +
                chart.chartArea.right
            ) / 2;

        const centerY =
            (
                chart.chartArea.top +
                chart.chartArea.bottom
            ) / 2;


        // =====================================================
        // DARK MODE
        // =====================================================

        const isDarkMode =
            document.documentElement
                .classList
                .contains("dark-mode");


        const numberColor =
            isDarkMode
                ? "#FFFFFF"
                : "#172B4D";

        const labelColor =
            isDarkMode
                ? "#D0D0D0"
                : "#7B8794";


        // =====================================================
        // DRAW CENTER TEXT
        // =====================================================

        ctx.save();


        // Number

        ctx.font =
            "600 24px Arial";

        ctx.fillStyle =
            numberColor;

        ctx.textAlign =
            "center";

        ctx.textBaseline =
            "middle";

        ctx.fillText(
            total,
            centerX,
            centerY - 7
        );


        // "Total"

        ctx.font =
            "400 11px Arial";

        ctx.fillStyle =
            labelColor;

        ctx.fillText(
            "Total",
            centerX,
            centerY + 14
        );


        ctx.restore();
    }
};


// =============================================================
// CREATE DOUGHNUT CHART
// =============================================================

function createDashboardDoughnut(canvas) {

    if (!canvas) {
        return null;
    }


    // =========================================================
    // DATA
    // =========================================================

    const approved =
        Number(
            canvas.dataset.approved || 0
        );

    const pending =
        Number(
            canvas.dataset.pending || 0
        );

    const rejected =
        Number(
            canvas.dataset.rejected || 0
        );


    // =========================================================
    // CHART
    // =========================================================

    return new Chart(
        canvas,
        {

            type: "doughnut",


            data: {

                labels: [
                    "Approved",
                    "Pending",
                    "Rejected"
                ],


                datasets: [
                    {

                        data: [
                            approved,
                            pending,
                            rejected
                        ],


                        // Keep existing PetHub colors

                        backgroundColor: [
                            "#8ACE79",
                            "#F2C94C",
                            "#E57373"
                        ],


                        borderWidth: 0

                    }
                ]
            },


            options: {

                responsive: true,

                maintainAspectRatio: false,

                cutout: "65%",


                plugins: {

                    // We already have your custom
                    // horizontal HTML legend.

                    legend: {
                        display: false
                    },


                    tooltip: {
                        enabled: true
                    }
                }
            },


            plugins: [
                doughnutCenterText
            ]
        }
    );
}


// =============================================================
// MARKETPLACE CHART
// =============================================================

const marketplaceCanvas =
    document.getElementById(
        "marketplaceChart"
    );

const marketplaceChart =
    createDashboardDoughnut(
        marketplaceCanvas
    );


// =============================================================
// LOST & FOUND CHART
// =============================================================

const lostFoundCanvas =
    document.getElementById(
        "lostFoundChart"
    );

const lostFoundChart =
    createDashboardDoughnut(
        lostFoundCanvas
    );