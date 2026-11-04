import { Routes } from '@angular/router';
import { TestListComponent } from './test-list/test-list.component';

export const TEST_ROUTES: Routes = [
  {
    path: '',
    component: TestListComponent
  },
  {
    path: 'new',
    loadComponent: () => import('./test-builder/test-builder.component').then(m => m.TestBuilderComponent)
  },
  {
    path: 'level-mapping',
    loadComponent: () => import('./level-mapping/level-mapping.component').then(m => m.LevelMappingComponent)
  },
  {
    path: ':id/analysis',
    loadComponent: () => import('./item-analysis/item-analysis.component').then(m => m.ItemAnalysisComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./test-builder/test-builder.component').then(m => m.TestBuilderComponent)
  }
];
