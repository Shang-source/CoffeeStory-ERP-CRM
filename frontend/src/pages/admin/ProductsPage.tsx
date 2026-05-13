import { useEffect, useState } from 'react';
import { Box, Typography, Card, CardContent, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Chip, IconButton, Alert, CircularProgress } from '@mui/material';
import { Add, Archive, Edit } from '@mui/icons-material';
import { toast } from 'sonner';
import { Product } from '@/entities/types';
import ProductDialog from '@/features/productCreateEdit/ui/ProductDialog';
import ConfirmDialog from '@/shared/ui/ConfirmDialog/ConfirmDialog';
import { getAdminProducts } from '@/entities/product/api/productApi';
import { archiveAdminProduct } from '@/features/productArchive/api/productArchiveApi';
import { createAdminProduct, type ProductPayload, updateAdminProduct } from '@/features/productCreateEdit/api/productCreateEditApi';

export default function Products() {
  const [products, setProducts] = useState<Product[]>([]);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [productToArchive, setProductToArchive] = useState<Product | null>(null);
  const [isArchiving, setIsArchiving] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadProducts = async () => {
      try {
        setError('');
        setProducts(await getAdminProducts());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load products');
      } finally {
        setIsLoading(false);
      }
    };

    void loadProducts();
  }, []);

  const handleAdd = () => {
    setSelectedProduct(null);
    setDialogOpen(true);
  };

  const handleEdit = (product: Product) => {
    setSelectedProduct(product);
    setDialogOpen(true);
  };

  const handleSave = async (productPayload: ProductPayload) => {
    const savedProduct = selectedProduct
      ? await updateAdminProduct(selectedProduct.id, productPayload)
      : await createAdminProduct(productPayload);

    setProducts((currentProducts) => {
      const nextProducts = selectedProduct
        ? currentProducts.map(product => product.id === savedProduct.id ? savedProduct : product)
        : [...currentProducts, savedProduct];
      return nextProducts.sort((a, b) => a.name.localeCompare(b.name));
    });
  };

  const handleArchive = async () => {
    if (!productToArchive) {
      return;
    }

    try {
      setIsArchiving(true);
      const archivedProduct = await archiveAdminProduct(productToArchive.id);
      setProducts((currentProducts) =>
        currentProducts.map((item) => item.id === archivedProduct.id ? archivedProduct : item)
      );
      toast.success(`${archivedProduct.name} archived`);
      setProductToArchive(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to archive product');
    } finally {
      setIsArchiving(false);
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <div>
          <Typography variant="h4" gutterBottom>
            Products
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage your coffee products
          </Typography>
        </div>
        <Button variant="contained" startIcon={<Add />} onClick={handleAdd}>
          Add Product
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

      <Card>
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>SKU</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Description</TableCell>
                  <TableCell>Unit</TableCell>
                  <TableCell align="right">Price</TableCell>
                  <TableCell align="right">Cost</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {products.map((product) => (
                  <TableRow key={product.id}>
                    <TableCell>{product.sku}</TableCell>
                    <TableCell>{product.name}</TableCell>
                    <TableCell>{product.description}</TableCell>
                    <TableCell>{product.unit}</TableCell>
                    <TableCell align="right">${product.price.toFixed(2)}</TableCell>
                    <TableCell align="right">${product.cost.toFixed(2)}</TableCell>
                    <TableCell>
                      <Chip label={product.isActive ? 'Active' : 'Inactive'} color={product.isActive ? 'success' : 'default'} size="small" />
                    </TableCell>
                    <TableCell align="center">
                      <IconButton size="small" onClick={() => handleEdit(product)}>
                        <Edit />
                      </IconButton>
                      <IconButton size="small" onClick={() => setProductToArchive(product)} disabled={!product.isActive} title="Archive product">
                        <Archive />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>
      <ProductDialog
        open={dialogOpen}
        product={selectedProduct}
        onClose={() => setDialogOpen(false)}
        onSave={handleSave}
      />
      <ConfirmDialog
        open={Boolean(productToArchive)}
        title="Archive Product"
        message={productToArchive ? `Archive ${productToArchive.name}? It will be hidden from new standing-order item selection.` : ''}
        confirmLabel="Archive"
        confirmColor="warning"
        isConfirming={isArchiving}
        onCancel={() => setProductToArchive(null)}
        onConfirm={handleArchive}
      />
    </Box>
  );
}
