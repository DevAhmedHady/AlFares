import { CarResponse, CarType, ClientResponse, OwnerType } from './models';

/** One selectable account an expense or revenue can be linked to. */
export interface OwnerEntityOption { id: string; label: string; }

/** Account-link choices shared by the expense and revenue forms. */
export const ownerLinkOptions: { label: string; value: OwnerType }[] = [
  { label: 'غير مرتبط', value: OwnerType.General },
  { label: 'عميل', value: OwnerType.Client },
  { label: 'سيارة مملوكة', value: OwnerType.OwnedCar },
  { label: 'سيارة مؤجرة', value: OwnerType.RentedCar },
];

/** Maps the loaded clients/cars to the entities valid for the chosen owner type. */
export function ownerEntityOptions(ownerType: OwnerType, clients: ClientResponse[], cars: CarResponse[]): OwnerEntityOption[] {
  if (ownerType === OwnerType.Client) return clients.map(c => ({ id: c.id, label: c.name }));
  if (ownerType === OwnerType.OwnedCar || ownerType === OwnerType.RentedCar) {
    const carType = ownerType === OwnerType.OwnedCar ? CarType.Owned : CarType.Rented;
    return cars.filter(c => c.type === carType).map(c => ({ id: c.id, label: c.plateNumber ? `${c.name} · ${c.plateNumber}` : c.name }));
  }
  return [];
}
