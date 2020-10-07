import React from 'react';
import './Chat.css';

import {
  Icon, Button, Label, Menu
} from 'semantic-ui-react';

const ConversationMenu = ({ conversations, active, onConversationChange }) => {
  const names = Object.keys(conversations);

  const unread = Object.entries(conversations).reduce((acc, [name, messages]) => {
    acc[name] = messages.filter(message => !message.acknowledged)
    return acc;
  }, {});

  const isActive = (name) => active === name;

  return (
    <Menu className='conversation-menu' stackable size='large'>
      {names.map((name, index) => 
        <Menu.Item
          className={`menu-item ${isActive(name) ? 'menu-active' : ''}`}
          key={index}
          name={name}
          active={isActive(name)}
          onClick={() => onConversationChange(name)}
        >
          {name}
          {(unread[name] || []).length === 0 ? 
            '' :
            <Label color='red'>{(unread[name] || []).length}</Label>
          }
        </Menu.Item>
      )}
      <Menu.Menu position='right'>
          <Button icon className='add-button'><Icon name='plus'/></Button>
      </Menu.Menu>
    </Menu>
  )
}

export default ConversationMenu;