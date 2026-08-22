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
    
    $result = "PASS"
    $issues = @()
    
    for ($i = 1; $i -lt $cashFlows.Count; $i++) {
        $prev = $cashFlows[$i - 1]
        $curr = $cashFlows[$i]
        
        if ($curr.openingBalance -ne $prev.closingBalance) {
            $result = "FAIL"
            $issues += "Balance mismatch at row $i"
        }
        
        if ($curr.event -eq "Interest" -or $curr.event -eq "Compounding Interest") {
            if ($curr.cashFlowAmount -eq 0) {
                $result = "FAIL"
                $issues += "Zero cash flow at row $i"
            }
            if ($curr.cashFlowAmount -ne $curr.interestAmount) {
                $result = "FAIL"
                $issues += "CashFlowAmount != InterestAmount at row $i"
            }
        }
    }
    
    $lastRow = $cashFlows[-1]
    if ($lastRow.event -ne "Maturity") {
        $result = "FAIL"
        $issues += "Last row not Maturity"
    }
    if ($lastRow.closingBalance -ne 0) {
        $result = "FAIL"
        $issues += "Final closing balance not 0"
    }
    
    $issueStr = if ($issues.Count -gt 0) { $issues -join "; " } else { "None" }
    Write-Host "$intFreq | $compFreq | $result | $issueStr"
}
