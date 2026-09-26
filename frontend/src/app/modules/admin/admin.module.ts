import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AdminRoutingModule } from './admin-routing.module';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { KnowledgeBaseComponent } from './components/knowledge-base/knowledge-base.component';


@NgModule({
  declarations: [DashboardComponent, KnowledgeBaseComponent],
  imports: [
    CommonModule,
    AdminRoutingModule
  ]
})
export class AdminModule { }
