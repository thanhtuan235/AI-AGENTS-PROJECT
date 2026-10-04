import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminRoutingModule } from './admin-routing.module';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { KnowledgeBaseComponent } from './components/knowledge-base/knowledge-base.component';
import { SharedMaterialModule } from '../../shared/shared-material.module';

@NgModule({
  declarations: [DashboardComponent, KnowledgeBaseComponent],
  imports: [CommonModule, AdminRoutingModule, SharedMaterialModule],
})
export class AdminModule {}
