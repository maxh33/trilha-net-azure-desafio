# Azure Deployment Configuration Guide

This guide walks you through fixing the 500 error when deploying to Azure App Service.

## Problem Summary

When deploying to Azure, the API returns HTTP 500 errors because:
1. `appsettings.json` is in `.gitignore` (not deployed with sensitive data)
2. Azure App Service needs proper configuration to override empty connection strings
3. SQL Server firewall might be blocking Azure App Service IPs

## Solution Steps

### Step 1: Configure SQL Server Firewall ⚡ **CRITICAL**

**Problem:** Azure App Service cannot connect to your SQL Server.

**Fix:**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your SQL Server: **`maxh`** (in resource group `DesafioDio`)
3. Click **"Networking"** (left menu)
4. Under **"Firewall rules"**, check the box:
   - ✅ **"Allow Azure services and resources to access this server"**
5. Click **"Save"**
6. Wait for confirmation

This allows your App Service (`app-desafio-dio-etfvdmcbfmgfb2c6`) to connect to SQL Database.

---

### Step 2: Verify App Service Configuration

Your connection strings are already configured, but let's verify they're correct:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **App Service** → **`App-desafio-dio`**
3. Click **"Configuration"** (left menu under Settings)
4. Click **"Connection strings"** tab

**Verify these exist:**

| Name | Value | Type |
|------|-------|------|
| `ConexaoPadrao` | `Server=tcp:maxh.database.windows.net,1433;Initial Catalog=dio-net-azure-desafio;...` | SQLAzure |
| `SAConnectionString` | `DefaultEndpointsProtocol=https;AccountName=storageaccountdioazure;...` | Custom |

**Add this one (if missing):**

| Name | Value | Type |
|------|-------|------|
| `AzureTableName` | `FuncionarioLog` | Custom |

4. Click **"Save"** at the top
5. Click **"Continue"** when prompted to restart

---

### Step 3: Test Connection Endpoint

After Step 1 & 2, deploy your updated code and test the connection:

**Test URL:**
```
GET https://app-desafio-dio-etfvdmcbfmgfb2c6.canadacentral-01.azurewebsites.net/Funcionario/TestConnection
```

**Expected Response (Success):**
```json
{
  "sqlDatabase": {
    "connected": true,
    "message": "SQL connection successful"
  },
  "tableStorage": {
    "hasConnectionString": true,
    "hasTableName": true,
    "tableName": "FuncionarioLog",
    "connectionStringPrefix": "DefaultEndpointsProtocol=https..."
  }
}
```

**If SQL connection fails:**
- Double-check Step 1 (firewall rule)
- Verify SQL Server credentials in connection string
- Check SQL Server is online

**If Table Storage has missing values:**
- Verify Step 2 (connection strings configured)
- Make sure `AzureTableName` is added

---

### Step 4: Deploy Updated Code

Now deploy the updated code with the test endpoint:

**Using VS Code Azure Extension:**
1. Right-click on `App-desafio-dio` in Azure App Service panel
2. Click **"Deploy to Web App..."**
3. Select your project folder
4. Wait for deployment to complete
5. App will automatically restart

**Using Git (if configured):**
```bash
git add .
git commit -m "fix: Add test endpoint and remove secrets from appsettings.json"
git push azure main
```

---

### Step 5: Enable Detailed Logging (for troubleshooting)

If you still get 500 errors, enable detailed logs:

1. Go to **App Service** → **`App-desafio-dio`**
2. Click **"App Service logs"** (left menu under Monitoring)
3. Set **"Application logging (Filesystem)"** to **"Information"**
4. Click **"Save"**
5. Click **"Log stream"** (left menu under Monitoring)
6. Try your POST request again
7. Watch the logs for detailed error messages

---

## Troubleshooting Common Issues

### Issue 1: Still getting 500 errors after firewall fix

**Diagnosis:**
```bash
GET /Funcionario/TestConnection
```

Look at the response to see what's failing.

**Solutions:**
- If `sqlDatabase.connected = false`: Check SQL firewall and credentials
- If `tableStorage.hasConnectionString = false`: Check App Service Configuration (Step 2)

---

### Issue 2: SQL connection timeout

**Symptoms:** Request takes 30+ seconds then fails

**Solution:**
1. Verify SQL Server is in same region or nearby (West US 2 vs Canada Central = higher latency)
2. Check if SQL Server is paused (Serverless tier auto-pauses)
3. In Azure Portal → SQL Database → Overview, check status is "Online"

---

### Issue 3: "Cannot open server" error

**Error:** `Cannot open server 'maxh' requested by the login. Client with IP address 'X.X.X.X' is not allowed to access the server.`

**Solution:**
- You need Step 1 (firewall rule) to be enabled
- Alternative: Add specific App Service outbound IPs to firewall
  - Get IPs from App Service → Properties → "Outbound IP addresses"
  - Add each IP as a firewall rule in SQL Server

---

### Issue 4: Table Storage connection string not found

**Error in logs:** `Value cannot be null. Parameter name: connectionString`

**Solution:**
The app is reading empty string from `appsettings.json` instead of Environment Variables.

**Fix Option A - Verify App Service Config:**
1. App Service → Configuration → Connection strings
2. Ensure `SAConnectionString` type is **"Custom"** (not "SQLAzure")
3. Save and restart

**Fix Option B - Change code to use GetConnectionString():**

Edit `FuncionarioController.cs` line 19:
```csharp
// FROM:
_connectionString = configuration.GetValue<string>("ConnectionStrings:SAConnectionString");

// TO:
_connectionString = configuration.GetConnectionString("SAConnectionString");
```

---

## Verification Checklist

Before testing POST endpoint:

- [ ] SQL Server firewall: "Allow Azure services" is enabled
- [ ] App Service Configuration has all 3 connection strings
- [ ] `appsettings.json` has empty strings (no secrets)
- [ ] Code is deployed to Azure
- [ ] `/Funcionario/TestConnection` returns success for both SQL and Table Storage
- [ ] App Service logs are enabled (for debugging)

---

## Expected Flow After Fix

1. **POST** `/Funcionario` → Creates employee in SQL Database → Logs to Table Storage → Returns 201 Created
2. **GET** `/Funcionario/Logs` → Returns audit logs from Table Storage
3. **GET** `/Funcionario/{id}` → Returns employee from SQL Database

---

## Security Notes

**Why `appsettings.json` has empty strings:**
- Secrets should NEVER be committed to source control
- Azure App Service Configuration **overrides** `appsettings.json` values
- This follows Azure best practices for secret management

**For Production:**
- Consider using **Azure Key Vault** for secrets
- Enable **Managed Identity** for App Service
- Use **Azure SQL Database** with AAD authentication

---

## Quick Reference

**Your Resources:**
- App Service: `app-desafio-dio-etfvdmcbfmgfb2c6.canadacentral-01.azurewebsites.net`
- SQL Server: `maxh.database.windows.net`
- SQL Database: `dio-net-azure-desafio`
- Storage Account: `storageaccountdioazure`
- Table Name: `FuncionarioLog`

**Useful URLs:**
- Swagger: `https://app-desafio-dio-etfvdmcbfmgfb2c6.canadacentral-01.azurewebsites.net/swagger`
- Test Endpoint: `https://app-desafio-dio-etfvdmcbfmgfb2c6.canadacentral-01.azurewebsites.net/Funcionario/TestConnection`
- Log Stream: Azure Portal → App-desafio-dio → Log stream
