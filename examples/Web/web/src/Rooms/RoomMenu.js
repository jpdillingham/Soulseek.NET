import React from 'react';
import './Rooms.css';

import {
  Icon, Button, Menu
} from 'semantic-ui-react';
import JoinRoomModal from './JoinRoomModal';

const RoomMenu = ({ rooms, active, onRoomChange, ...rest }) => {
  const names = Object.keys(rooms);
  const isActive = (name) => active === name;

  return (
    <Menu className='room-menu' stackable size='large'>
      {names.map((name, index) => 
        <Menu.Item
          className={`menu-item ${isActive(name) ? 'menu-active' : ''}`}
          key={index}
          name={name}
          active={isActive(name)}
          onClick={() => onRoomChange(name)}
        >
          <Icon name='circle' size='tiny' color='green'/>
          {name}
        </Menu.Item>
      )}
      <Menu.Menu position='right'>
        <JoinRoomModal
          trigger={
            <Button icon className='add-button'><Icon name='plus'/></Button>
          }
          centered
          size='mini'
          {...rest}
        />
      </Menu.Menu>
    </Menu>
  )
}

export default RoomMenu;