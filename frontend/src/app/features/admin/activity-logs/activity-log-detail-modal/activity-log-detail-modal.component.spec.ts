import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ActivityLogDetailModalComponent } from './activity-log-detail-modal.component';

describe('ActivityLogDetailModalComponent', () => {
  let component: ActivityLogDetailModalComponent;
  let fixture: ComponentFixture<ActivityLogDetailModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ActivityLogDetailModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ActivityLogDetailModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
