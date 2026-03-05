import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { CollectionService } from '../Services/collection.service';
import { ProductService } from '../Services/product.service';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-admin-collection',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-collection.html',
  styleUrl: './admin-collection.css'
})
export class AdminCollection implements OnInit {
  collections: any[] = [];
  allProducts: any[] = [];
  filteredProducts: any[] = [];

  searchSubject = new Subject<string>();
  searchQuery: string = '';

  showModal = false;
  isEditMode = false;
  currentCollection: any = {
    id: '',
    name: '',
    description: '',
    showOnHomePage: false,
    displayOrder: 0,
    productIds: [] 
  };

  constructor(
    private collectionService: CollectionService,
    private productService: ProductService
  )
  {
    this.searchSubject.pipe(
      debounceTime(800),
      distinctUntilChanged()
    ).subscribe(query => {
      this.performSearch(query);
    });
  }

  ngOnInit(): void {
    this.loadCollections();
    this.loadAllProducts();
  }

  loadCollections() {
    this.collectionService.getAllCollectionsAdmin().subscribe({
      next: (res) => {
        const data = Array.isArray(res) ? res : (res.value || res.data);
        this.collections = data || [];
      },
      error: (err) => console.error('Lỗi tải danh sách Collection', err)
    });
  }

  loadAllProducts() {
    this.productService.getProducts(1, 100).subscribe({
      next: (res) => {
        const data = res.value || res.data;
        this.allProducts = data?.items || [];

        this.filteredProducts = [...this.allProducts];
      }
    });
  }

  onSearchChange(query: string) {
    this.searchQuery = query;
    this.searchSubject.next(query);
  }

  performSearch(query: string) {
    if (!query || query.trim() === '') {
      this.filteredProducts = [...this.allProducts];
      return;
    }

    const lowerQuery = query.toLowerCase();
    this.filteredProducts = this.allProducts.filter(p =>
      p.name.toLowerCase().includes(lowerQuery)
    );
  }

  openAddModal() {
    this.isEditMode = false;
    this.currentCollection = { name: '', description: '', showOnHomePage: false, displayOrder: 0, productIds: [] };
    this.searchQuery = ''; 
    this.filteredProducts = [...this.allProducts];
    this.showModal = true;
  }

  openEditModal(collection: any) {
    this.isEditMode = true;
    this.currentCollection = JSON.parse(JSON.stringify(collection));
    if (!this.currentCollection.productIds) {
      this.currentCollection.productIds = [];
    }
    this.searchQuery = '';
    this.filteredProducts = [...this.allProducts];
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
  }

  toggleProductSelection(productId: string, event: any) {
    const isChecked = event.target.checked;
    if (isChecked) {
      this.currentCollection.productIds.push(productId);
    } else {
      this.currentCollection.productIds = this.currentCollection.productIds.filter((id: string) => id !== productId);
    }
  }

  saveCollection() {
    if (!this.currentCollection.name.trim()) {
      Swal.fire('Error', 'Please enter the collection name.', 'warning');
      return;
    }

    const requestData = {
      name: this.currentCollection.name,
      description: this.currentCollection.description || '',
      showOnHomePage: this.currentCollection.showOnHomePage === true, // Forces absolute boolean
      displayOrder: parseInt(this.currentCollection.displayOrder, 10) || 0, // Forces absolute integer
      isActive: true,
      productIds: Array.isArray(this.currentCollection.productIds) ? this.currentCollection.productIds : [] // Forces absolute array
    };

    if (this.isEditMode) {
      this.collectionService.updateCollection(this.currentCollection.id, requestData).subscribe({
        next: () => {
          Swal.fire('Successful!', 'Collection has been updated.', 'success');
          this.closeModal();
          this.loadCollections();
        },
        error: () => Swal.fire('Error', 'Can not Update', 'error')
      });
    } else {
      this.collectionService.createCollection(requestData).subscribe({
        next: () => {
          Swal.fire('Successful!', 'Collection created', 'success');
          this.closeModal();
          this.loadCollections();
        },
        error: () => Swal.fire('Error', 'Can not Create', 'error')
      });
    }
  }

  deleteCollection(id: string) {
    Swal.fire({
      title: 'Delete Collection?',
      text: "This action does not delete the original product, only the group.",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Delete!',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.collectionService.deleteCollection(id).subscribe({
          next: () => {
            Swal.fire('Deleted!', 'The collection has been deleted.', 'success');
            this.loadCollections();
          },
          error: () => Swal.fire('Error', 'Can not delete', 'error')
        });
      }
    });
  }
}
