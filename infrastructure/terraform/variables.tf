variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
}

variable "location" {
  description = "Azure region para todos los recursos"
  type        = string
  default     = "eastus"
}

variable "resource_group_name" {
  description = "Nombre del Resource Group"
  type        = string
  default     = "rg-gorrashop-lab"
}

variable "environment" {
  description = "Entorno (lab, dev, prod)"
  type        = string
  default     = "lab"
}

# ─── AKS ──────────────────────────────────────────────────────────────────────
variable "cluster_name" {
  description = "Nombre del cluster AKS"
  type        = string
  default     = "aks-gorrashop"
}

variable "kubernetes_version" {
  description = "Versión de Kubernetes. null = última estable disponible"
  type        = string
  default     = null
}

variable "system_node_count" {
  description = "Número de nodos en el pool de sistema"
  type        = number
  default     = 2
}

variable "system_node_vm_size" {
  description = "VM size del pool de sistema"
  type        = string
  default     = "Standard_D2s_v3"
}

variable "app_node_count" {
  description = "Número de nodos en el pool de aplicaciones"
  type        = number
  default     = 2
}

variable "app_node_vm_size" {
  description = "VM size del pool de aplicaciones"
  type        = string
  default     = "Standard_D2s_v3"
}

# ─── ACR ──────────────────────────────────────────────────────────────────────
variable "acr_name" {
  description = "Nombre del Azure Container Registry (debe ser globalmente único, solo alfanumérico)"
  type        = string
  default     = "acrgorrashoplab"
}

variable "acr_sku" {
  description = "SKU del ACR: Basic, Standard, Premium"
  type        = string
  default     = "Standard"
}

# ─── Networking ───────────────────────────────────────────────────────────────
variable "vnet_address_space" {
  description = "Espacio de direcciones del VNet"
  type        = string
  default     = "10.0.0.0/8"
}

variable "aks_subnet_prefix" {
  description = "CIDR del subnet de AKS"
  type        = string
  default     = "10.240.0.0/16"
}

# ─── Tags ─────────────────────────────────────────────────────────────────────
variable "tags" {
  description = "Tags aplicados a todos los recursos"
  type        = map(string)
  default = {
    project     = "gorrashop"
    environment = "lab"
    managed_by  = "terraform"
    datadogsg   = "true"
    team        = "allqu"
  }
}
