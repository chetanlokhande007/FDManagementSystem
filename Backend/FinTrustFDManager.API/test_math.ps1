$configs = @(
    @("MONTHLY", "MONTHLY"),
    @("MONTHLY", "QUARTERLY"),
    @("MONTHLY", "HALF_YEARLY"),
    @("MONTHLY", "ANNUALLY"),
    @("HALF_YEARLY", "MONTHLY"),
    @("HALF_YEARLY", "HALF_YEARLY"),
    @("QUARTERLY", "QUARTERLY"),
    @("ANNUALLY", "ANNUALLY")
)

$results = @()

foreach ($config in $configs) {
    $intFreq = $config[0]
    $compFreq = $config[1]
    
    $body = @{
        fdInterestId = 120
        fdId = 126
        interestRateType = "FIXED"
        interestRate = 7
        interestFrequency = $intFreq
        compoundingFrequency = $compFreq
        isCompounding = $true
        calculationBasis = "ACTUAL_365"
        paymentConvention = "Cash"
    } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "http://localhost:5075/api/FDInterest/120" -Method Put -Body $body -ContentType "application/json" | Out-Null
    
    $cashFlows = Invoke-RestMethod -Uri "http://localhost:5075/api/FDCashFlow/fd/126" -Method Get
    
    $interestPeriods = 0
    $compoundingDates = 0
    $finalPrincipal = 0
    $totalInterest = 0
    
    foreach ($cf in $cashFlows) {
        if ($cf.event -eq "Interest") {
            $interestPeriods++
            $totalInterest += $cf.interestAmount
        }
        elseif ($cf.event -eq "Compounding Interest") {
            $compoundingDates++
            $interestPeriods++
            $totalInterest += $cf.interestAmount
            $finalPrincipal = $cf.closingBalance
        }
        elseif ($cf.event -eq "Maturity") {
            $finalPrincipal = $cf.cashFlowAmount
        }
    }
    
    Write-Host "$intFreq + $compFreq"
    Write-Host "Actual interest periods: $interestPeriods"
    Write-Host "Actual compounding dates: $compoundingDates"
    Write-Host "Actual final principal: $finalPrincipal"
    Write-Host "Actual total interest: $totalInterest"
    Write-Host "------------------------"
}
